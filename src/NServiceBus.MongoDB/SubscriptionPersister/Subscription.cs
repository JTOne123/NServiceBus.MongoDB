﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Subscription.cs" company="SharkByte Software">
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
//   The subscription.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NServiceBus.MongoDB.SubscriptionPersister
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using NServiceBus.MongoDB.Internals;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    internal sealed class Subscription : IHaveDocumentVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class.
        /// </summary>
        public Subscription()
        {
            this.Id = string.Empty;
            this.DocumentVersion = 0;
            this.MessageType = new MessageType(typeof(object));
            this.Subscribers = new List<Subscriber>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class.
        /// </summary>
        /// <param name="messageType">
        /// The message type.
        /// </param>
        public Subscription(MessageType messageType)
        {
            Contract.Requires(messageType != null, "messageType != null");

            this.Id = FormatId(messageType);
            this.MessageType = messageType;
            this.Subscribers = new List<Subscriber>();
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the document version.
        /// </summary>
        public int DocumentVersion { get; set; }

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Gets or sets the subscribers
        /// </summary>
        public List<Subscriber> Subscribers { get; set; }

        /// <summary>
        /// The format id.
        /// </summary>
        /// <param name="messageType">
        /// The message type.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string FormatId(MessageType messageType)
        {
            Contract.Requires<ArgumentNullException>(messageType != null, "messageType != null");
            Contract.Ensures(Contract.Result<string>() != null);

            var id = DeterministicGuid.Create(messageType.TypeName, "/", messageType.Version.Major);
            return string.Format("Subscriptions/{0}", id);
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(this.Id != null);
            Contract.Invariant(this.MessageType != null);
            Contract.Invariant(this.Subscribers != null);
        }
    }
}
