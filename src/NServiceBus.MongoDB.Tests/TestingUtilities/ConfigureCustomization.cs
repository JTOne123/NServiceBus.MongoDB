﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigureCustomization.cs" company="Carlos Sandoval">
//   The MIT License (MIT)
//   
//   Copyright (c) 2014 Carlos Sandoval
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
//   Defines the ConfigureCustomization type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NServiceBus.MongoDB.Tests.TestingUtilities
{
    using NServiceBus.Timeout.Core;

    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Kernel;

    public class ConfigureCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var config = Configure.With(new[] { GetType().Assembly })
                                  .DefineEndpointName("Unit.Tests")
                                  .DefaultBuilder();

            fixture.Register(() => config);

            fixture.Customize<Address>(c => c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));

            fixture.Customize<TimeoutData>(c => c.With(t => t.OwningTimeoutManager, Configure.EndpointName));
        }
    }
}
