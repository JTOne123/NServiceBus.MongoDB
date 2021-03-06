﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MongoSagaPersisterTests.cs" company="SharkByte Software">
//   The MIT License (MIT)
//   
//   Copyright (c) 2018 SharkByte Software
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
// --------------------------------------------------------------------------------------------------------------------

namespace NServiceBus.MongoDB.Tests.SagaPersister
{
    using System;
    using AutoFixture.Xunit2;
    using FluentAssertions;

    using global::MongoDB.Driver;

    using NServiceBus.Extensibility;
    using NServiceBus.MongoDB.Internals;
    using NServiceBus.MongoDB.SagaPersister;
    using NServiceBus.MongoDB.Tests.Sample;
    using NServiceBus.MongoDB.Tests.TestingUtilities;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;

    using Xunit;

    public class MongoSagaPersisterTests
    {
        [Theory]
        [AutoDatabase]
        public void BasicMongoSagaPersisterConstruction(MongoDatabaseFactory factory)
        {
            var sut = new MongoSagaPersister(factory);
            sut.Should().NotBeNull();
        }

        [Theory]
        [AutoDatabase]
        public void SavingSagaWithUniqueProperty(
            MongoSagaPersister sut,
            MongoDatabaseFactory factory,
            SagaWithUniqueProperty sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Save(sagaData, correlationProperty, session, context).Wait();

            var entity = factory.RetrieveSagaData(sagaData);

            entity.Id.Should().Be(sagaData.Id);
            entity.UniqueProperty.Should().Be(sagaData.UniqueProperty);
            entity.SomeValue.Should().Be(sagaData.SomeValue);
        }

        [Theory]
        [AutoDatabase]
        public void InterleavedSavingSagaShouldThrowException(
            MongoSagaPersister sut,
            SagaWithUniqueProperty sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Save(sagaData, correlationProperty, session, context).Wait();
            sut.Invoking(s => s.Save(sagaData, correlationProperty, session, context).Wait())
                .Should().Throw<MongoWriteException>();
        }

        [Theory]
        [AutoDatabase]
        public void SavingSagaWithSameUniquePropertyAsAnAlreadyCompletedSaga(
            MongoSagaPersister sut,
            MongoDatabaseFactory factory,
            SagaWithUniqueProperty sagaData1,
            SagaWithUniqueProperty sagaData2,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            var uniqueProperty = Guid.NewGuid().ToString();
            sagaData1.UniqueProperty = uniqueProperty;
            sagaData2.UniqueProperty = uniqueProperty;

            sut.Save(sagaData1, correlationProperty, session, context).Wait();
            sut.Complete(sagaData1, session, context).Wait();

            sut.Save(sagaData2, correlationProperty, session, context).Wait();

            var entity = factory.RetrieveSagaData(sagaData2);
            entity.UniqueProperty.Should().Be(sagaData2.UniqueProperty);
            entity.SomeValue.Should().Be(sagaData2.SomeValue);
        }

        [Theory]
        [AutoDatabase]
        public void SavingSagaWithoutDocumentVersionShouldThrowException(
            MongoSagaPersister sut,
            SagaWithoutDocumentVersion sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Invoking(s => s.Save(sagaData, correlationProperty, session, context).Wait())
                .Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [AutoDatabase]
        public void UpdatingSagaWithUniqueProperty(
            MongoSagaPersister sut,
            MongoDatabaseFactory factory,
            SagaWithUniqueProperty sagaData,
            string newValue,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Save(sagaData, correlationProperty, session, context).Wait();

            sagaData.SomeValue = newValue;
            sut.Update(sagaData, session, context).Wait();

            var entity = factory.RetrieveSagaData(sagaData);
            entity.SomeValue.Should().Be(newValue);
        }

        [Theory]
        [AutoDatabase]
        public void UpdatingNonExistantSagaWithUniqueProperty(
            MongoSagaPersister sut,
            MongoDatabaseFactory factory,
            SagaWithUniqueProperty sagaData,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Invoking(s => s.Update(sagaData, session, context).Wait()).Should().Throw<InvalidOperationException>();

            factory.RetrieveSagaData(sagaData).Should().BeNull();
        }

        [Theory]
        [AutoDatabase]
        public void UpdateCollisionShouldFail(
            MongoSagaPersister sut,
            MongoDatabaseFactory factory,
            SagaWithUniqueProperty sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Save(sagaData, correlationProperty, session, context).Wait();
            var saga1 = factory.RetrieveSagaData(sagaData);
            var saga2 = factory.RetrieveSagaData(sagaData);

            saga1.UniqueProperty = Guid.NewGuid().ToString();
            sut.Update(saga1, session, context).Wait();

            saga2.UniqueProperty = Guid.NewGuid().ToString();
            sut.Invoking(s => s.Update(saga2, session, context).Wait()).Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [AutoDatabase]
        public void UpdatingSagaWithoutDocumentVersion(
            MongoSagaPersister sut,
            SagaWithUniqueProperty sagaData,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Invoking(s => s.Update(sagaData, session, context).Wait()).Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [AutoDatabase]
        public void UpdatingSagaWithNoChangesShouldUpdateVersion(
            MongoSagaPersister sut,
            MongoDatabaseFactory factory,
            SagaWithUniqueProperty sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Save(sagaData, correlationProperty, session, context).Wait();
            var saga1 = factory.RetrieveSagaData(sagaData);

            sut.Update(saga1, session, context).Wait();

            var saga2 = factory.RetrieveSagaData(sagaData);
            saga2.DocumentVersion.Should().Be(saga1.DocumentVersion+1);
        }

        [Theory]
        [AutoDatabase]
        public void UpdatingSagaWithChangesShouldUpdateVersion(
                MongoSagaPersister sut,
                MongoDatabaseFactory factory,
                SagaWithUniqueProperty sagaData,
                SagaCorrelationProperty correlationProperty,
                SynchronizedStorageSession session,
                ContextBag context)
        {
            sut.Save(sagaData, correlationProperty, session, context).Wait();
            var saga1 = factory.RetrieveSagaData(sagaData);

            saga1.SomeValue = "NewValue";
            sut.Update(saga1, session, context).Wait();

            var saga2 = factory.RetrieveSagaData(sagaData);
            saga2.DocumentVersion.Should().Be(saga1.DocumentVersion+1);
            saga2.UniqueProperty.Should().Be(saga1.UniqueProperty);
            saga2.SomeValue.Should().Be(saga1.SomeValue);
        }

        [Theory]
        [AutoDatabase]
        public void CompletingSagaShouldRemoveDocument(
            MongoSagaPersister sut,
            MongoDatabaseFactory factory,
            SagaWithUniqueProperty sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Save(sagaData, correlationProperty, session, context).Wait();

            sut.Complete(sagaData, session, context).Wait();
            factory.RetrieveSagaData(sagaData).Should().BeNull();
        }

        [Theory]
        [AutoDatabase]
        public void RetrievingSagaUsingId(
            MongoSagaPersister sut,
            SagaWithUniqueProperty sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Save(sagaData, correlationProperty, session, context).Wait();

            var result = sut.Get<SagaWithUniqueProperty>(sagaData.Id, session, context).Result;

            result.Should().BeEquivalentTo(sagaData);
        }

        [Theory]
        [AutoDatabase]
        public void RetrievingSagaUsingIdNotFound(
            MongoSagaPersister sut,
            SagaWithUniqueProperty sagaData,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Get<SagaWithUniqueProperty>(sagaData.Id, session, context).Result.Should().BeNull();
        }

        [Theory]
        [AutoDatabase]
        public void RetrievingSagaUsingCorrelationProperty(
            MongoSagaPersister sut,
            SagaWithUniqueProperty sagaData,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sagaData.UniqueProperty = correlationProperty.Value.ToString();
            sut.Save(sagaData, correlationProperty, session, context).Wait();

            var result =
                sut.Get<SagaWithUniqueProperty>(
                    correlationProperty.Name,
                    correlationProperty.Value,
                    session,
                    context).Result;

            result.Should().BeEquivalentTo(sagaData);
        }

        [Theory]
        [AutoDatabase]
        public void RetrievingSagaUsingCorrelationPropertyNotFound(
            MongoSagaPersister sut,
            SagaCorrelationProperty correlationProperty,
            SynchronizedStorageSession session,
            ContextBag context)
        {
            sut.Get<SagaWithUniqueProperty>(correlationProperty.Name, "badvalue", session, context)
                .Result.Should()
                .BeNull();
        }
    }
}
