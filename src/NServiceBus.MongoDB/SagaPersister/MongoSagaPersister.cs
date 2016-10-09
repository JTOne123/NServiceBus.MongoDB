﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MongoSagaPersister.cs" company="SharkByte Software">
//   The MIT License (MIT)
//   
//   Copyright (c) 2015 SharkByte Software
//   
//   Permission is hereby granted, free of charge, to any person obtaining a copy of
//   this software and associated documentation files (the "Software"), to deal in
//   the Software without restriction, including without limitation the rights to
//   use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//   the Software, and to permit persons to whom the Software is furnished to do so,
//   subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all
//   copies or substantial portions of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
//   FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
//   COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
//   IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
//   CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   The Mongo saga persister.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NServiceBus.MongoDB.SagaPersister
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    using NServiceBus.Extensibility;
    using NServiceBus.MongoDB.Extensions;
    using NServiceBus.MongoDB.Internals;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;

    /// <summary>
    /// The Mongo saga persister.
    /// </summary>
    public class MongoSagaPersister : ISagaPersister
    {
        ////private static ConcurrentDictionary<Type, string> indexes = new ConcurrentDictionary<Type, string>();

        private readonly IMongoDatabase mongoDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoSagaPersister"/> class.
        /// </summary>
        /// <param name="mongoFactory">
        /// The mongo factory.
        /// </param>
        public MongoSagaPersister(MongoDatabaseFactory mongoFactory)
        {
            Contract.Requires<ArgumentNullException>(mongoFactory != null, "mongoFactory != null");
            this.mongoDatabase = mongoFactory.GetDatabase();
        }

        public async Task Save(
            IContainSagaData sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            var sagaTypeName = sagaData.GetType().Name;

            if (!(sagaData is IHaveDocumentVersion))
            {
                throw new InvalidOperationException(
                    string.Format("Saga type {0} does not implement IHaveDocumentVersion", sagaTypeName));
            }

            var sagaDataWithVersion = (IHaveDocumentVersion)sagaData;

            sagaDataWithVersion.DocumentVersion = 0;
            sagaDataWithVersion.ETag = sagaData.ComputeETag();

            if (correlationProperty != null)
            {
                await this.EnsureUniqueIndex(sagaData, correlationProperty);
            }

            var collection = this.mongoDatabase.GetCollection<BsonDocument>(sagaTypeName);
            await collection.InsertOneAsync(sagaData.ToBsonDocument()).ConfigureAwait(false);
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var newETag = sagaData.AssumedNotNull().ComputeETag();

            var versionedDocument = (IHaveDocumentVersion)sagaData;
            if (versionedDocument.ETag == newETag)
            {
                return Task.FromResult(0);
            }

            var query = sagaData.MongoUpdateQuery(versionedDocument.DocumentVersion);
            var update = sagaData.MongoUpdate(newETag);

            var collection = this.mongoDatabase.GetCollection<BsonDocument>(sagaData.GetType().Name);

            var result = collection.UpdateOneAsync(query, update).Result;

            if (result.ModifiedCount != 1)
            {
                throw new InvalidOperationException(string.Format("Unable to update saga with id {0}", sagaData.Id));
            }

            return Task.FromResult(0);
        }

        public Task<TSagaData> Get<TSagaData>(
            Guid sagaId, 
            SynchronizedStorageSession session, 
            ContextBag context) where TSagaData : IContainSagaData
        {
            var collection = this.mongoDatabase.GetCollection<TSagaData>(typeof(TSagaData).Name);

            var query = Builders<TSagaData>.Filter.Eq(e => e.Id, sagaId);
            return collection.FindAsync(query).Result.FirstAsync();
        }

        public Task<TSagaData> Get<TSagaData>(
            string propertyName, 
            object propertyValue, 
            SynchronizedStorageSession session, 
            ContextBag context) where TSagaData : IContainSagaData
        {
            var collection = this.mongoDatabase.GetCollection<TSagaData>(typeof(TSagaData).Name);

            var query = Builders<TSagaData>.Filter.Eq(propertyName, BsonValue.Create(propertyValue));
            return collection.FindAsync(query).Result.FirstAsync();
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var query = Builders<BsonDocument>.Filter.Eq(MongoPersistenceConstants.IdPropertyName, sagaData.Id);

            var collection = this.mongoDatabase.GetCollection<BsonDocument>(sagaData.GetType().Name);
            await collection.DeleteOneAsync(query).ConfigureAwait(false);
        }

        private Task EnsureUniqueIndex(IContainSagaData saga, SagaCorrelationProperty correlationProperty)
        {
            Contract.Requires(saga != null);

            var collection = this.mongoDatabase.GetCollection<BsonDocument>(saga.GetType().Name);

            return
                collection.Indexes.CreateOneAsync(
                    new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(correlationProperty.Name, 1)),
                    new CreateIndexOptions() { Unique = true });
        }
    }
}
