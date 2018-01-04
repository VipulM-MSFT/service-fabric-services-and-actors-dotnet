﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Client
{
    using System;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors.Remoting.V1;
    using Microsoft.ServiceFabric.Actors.Remoting.V1.Builder;
    using Microsoft.ServiceFabric.Actors.Remoting.V2;
    using Microsoft.ServiceFabric.Actors.Remoting.V2.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Builder;
    using Microsoft.ServiceFabric.Services.Remoting.V2;
    using IActorServicePartitionClient = Microsoft.ServiceFabric.Actors.Remoting.V1.Client.IActorServicePartitionClient;

    /// <summary>
    ///     Provides the base implementation for the proxy to the remote actor objects implementing <see cref="IActor" />
    ///     interfaces.
    ///     The proxy object can be used used for client-to-actor and actor-to-actor communication.
    /// </summary>
    public abstract class ActorProxy : ProxyBase, IActorProxy
    {
        internal static readonly ActorProxyFactory DefaultProxyFactory = new ActorProxyFactory();
        private ActorServicePartitionClient servicePartitionClientV2;

        internal RemotingClient RemotingClient { get; private set; }


        /// <summary>
        ///     Gets <see cref="ServiceFabric.Actors.ActorId" /> associated with the proxy object.
        /// </summary>
        /// <value><see cref="ServiceFabric.Actors.ActorId" /> associated with the proxy object.</value>
        public ActorId ActorId
        {
            get
            {
#if !DotNetCoreClr
                if (this.RemotingClient.Equals(RemotingClient.V1Client))
                {
                    return this.servicePartitionClient.ActorId;
                }
#endif
                return this.servicePartitionClientV2.ActorId;
            }
        }

#if !DotNetCoreClr
        /// <summary>
        ///     Gets the <see cref="Remoting.V1.Client.IActorServicePartitionClient" /> interface that this proxy is using to
        ///     communicate with the actor.
        /// </summary>
        /// <value>
        ///     <see cref="Remoting.V1.Client.IActorServicePartitionClient" /> that this proxy is using to communicate with the
        ///     actor.
        /// </value>
        public IActorServicePartitionClient ActorServicePartitionClient
        {
            get { return this.servicePartitionClient; }
        }
#endif

        /// <summary>
        ///     Gets the <see cref="Remoting.V2.Client.IActorServicePartitionClient" /> interface that this proxy is using to
        ///     communicate with the actor.
        /// </summary>
        /// <value>
        ///     <see cref="Remoting.V2.Client.IActorServicePartitionClient" /> that this proxy is using to communicate with the
        ///     actor.
        /// </value>
        public Remoting.V2.Client.IActorServicePartitionClient ActorServicePartitionClientV2
        {
            get { return this.servicePartitionClientV2; }
        }


        /// <summary>
        ///     Creates a proxy to the actor object that implements an actor interface.
        /// </summary>
        /// <typeparam name="TActorInterface">
        ///     The actor interface implemented by the remote actor object.
        ///     The returned proxy object will implement this interface.
        /// </typeparam>
        /// <param name="actorId">
        ///     The actor ID of the proxy actor object. Methods called on this proxy will result in requests
        ///     being sent to the actor with this ID.
        /// </param>
        /// <param name="applicationName">
        ///     The name of the Service Fabric application that contains the actor service hosting the actor objects.
        ///     This parameter can be null if the client is running as part of that same Service Fabric application. For more
        ///     information, see Remarks.
        /// </param>
        /// <param name="serviceName">
        ///     The name of the Service Fabric service as configured by <see cref="ActorServiceAttribute" /> on the actor
        ///     implementation.
        ///     By default, the name of the service is derived from the name of the actor interface. However,
        ///     <see cref="ActorServiceAttribute" />
        ///     is required when an actor implements more than one actor interface or an actor interface derives from another actor
        ///     interface since
        ///     the service name cannot be determined automatically.
        /// </param>
        /// <param name="listenerName">
        ///     By default an actor service has only one listener for clients to connect to and communicate with.
        ///     However, it is possible to configure an actor service with more than one listener. This parameter specifies the
        ///     name of the listener to connect to.
        /// </param>
        /// <returns>An actor proxy object that implements <see cref="IActorProxy" /> and TActorInterface.</returns>
        /// <remarks>
        ///     <para>
        ///         The applicationName parameter can be null if the client is running as part of the same Service Fabric
        ///         application as the actor service it intends to communicate with. In this case, the application name is
        ///         determined from
        ///         <see cref="System.Fabric.CodePackageActivationContext" />, and is obtained by calling the
        ///         <see cref="System.Fabric.CodePackageActivationContext.ApplicationName" /> property.
        ///     </para>
        /// </remarks>
        public static TActorInterface Create<TActorInterface>(
            ActorId actorId,
            string applicationName = null,
            string serviceName = null,
            string listenerName = null) where TActorInterface : IActor
        {
            return DefaultProxyFactory.CreateActorProxy<TActorInterface>(
                actorId,
                applicationName,
                serviceName,
                listenerName);
        }

        /// <summary>
        ///     Creates a proxy to the actor object that implements an actor interface.
        /// </summary>
        /// <typeparam name="TActorInterface">
        ///     The actor interface implemented by the remote actor object.
        ///     The returned proxy object will implement this interface.
        /// </typeparam>
        /// <param name="serviceUri">Uri of the actor service.</param>
        /// <param name="actorId">
        ///     Actor Id of the proxy actor object. Methods called on this proxy will result in requests
        ///     being sent to the actor with this id.
        /// </param>
        /// <param name="listenerName">
        ///     By default an actor service has only one listener for clients to connect to and communicate with.
        ///     However it is possible to configure an actor service with more than one listeners, the listenerName parameter
        ///     specifies the name of the listener to connect to.
        /// </param>
        /// <returns>An actor proxy object that implements <see cref="IActorProxy" /> and TActorInterface.</returns>
        public static TActorInterface Create<TActorInterface>(
            ActorId actorId,
            Uri serviceUri,
            string listenerName = null) where TActorInterface : IActor
        {
            return DefaultProxyFactory.CreateActorProxy<TActorInterface>(serviceUri, actorId, listenerName);
        }

        //V2 Stack Api

        internal void Initialize(
            ActorServicePartitionClient client,
            IServiceRemotingMessageBodyFactory serviceRemotingMessageBodyFactory)
        {
            this.servicePartitionClientV2 = client;
            this.InitializeV2(serviceRemotingMessageBodyFactory);
            this.RemotingClient = RemotingClient.V2Client;
        }


        internal override void InvokeImplV2(
            int interfaceId,
            int methodId,
            IServiceRemotingRequestMessageBody requestMsgBodyValue)
        {
            // no - op as events/one way messages are not supported for services
        }

        internal override Task<IServiceRemotingResponseMessage> InvokeAsyncImplV2(
            int interfaceId,
            int methodId,
            IServiceRemotingRequestMessageBody requestMsgBodyValue,
            CancellationToken cancellationToken)
        {
            var headers = new ActorRemotingMessageHeaders
            {
                ActorId = this.servicePartitionClientV2.ActorId,
                InterfaceId = interfaceId,
                MethodId = methodId,
                CallContext = Helper.GetCallContext()
            };

            return this.servicePartitionClientV2.InvokeAsync(
                new ServiceRemotingRequestMessage(
                    headers,
                    requestMsgBodyValue),
                cancellationToken);
        }


        internal async Task SubscribeAsyncV2(Type eventType, object subscriber, TimeSpan resubscriptionInterval)
        {
            ActorId actorId = this.servicePartitionClientV2.ActorId;
            SubscriptionInfo info = ActorEventSubscriberManager.Singleton.RegisterSubscriber(
                actorId,
                eventType,
                subscriber);

            Exception error = null;
            try
            {
                await this.servicePartitionClientV2.SubscribeAsync(info.Subscriber.EventId, info.Id);
            }
            catch (Exception e)
            {
                error = e;
            }

            if (error != null)
            {
                try
                {
                    await this.UnsubscribeAsyncV2(eventType, subscriber);
                }
                catch
                {
                    // ignore
                }

                throw error;
            }

            this.ResubscribeAsyncV2(info, resubscriptionInterval);
        }

        internal async Task UnsubscribeAsyncV2(Type eventType, object subscriber)
        {
            ActorId actorId = this.servicePartitionClientV2.ActorId;
            SubscriptionInfo info;
            if (ActorEventSubscriberManager.Singleton.TryUnregisterSubscriber(
                actorId,
                eventType,
                subscriber,
                out info))
            {
                await this.servicePartitionClientV2.UnsubscribeAsync(info.Subscriber.EventId, info.Id);
            }
        }

        private void ResubscribeAsyncV2(SubscriptionInfo info, TimeSpan resubscriptionInterval)
        {
#pragma warning disable 4014
            // ReSharper disable once UnusedVariable
            Task ignore = Task.Run(
                async () =>
#pragma warning restore 4014
                {
                    while (true)
                    {
                        await Task.Delay(resubscriptionInterval);

                        if (!info.IsActive)
                        {
                            break;
                        }

                        try
                        {
                            await
                                this.servicePartitionClientV2.SubscribeAsync(info.Subscriber.EventId, info.Id)
                                    .ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                });
        }

#if !DotNetCoreClr

        private ActorProxyGeneratorWith proxyGeneratorWith;
        private Remoting.V1.Client.ActorServicePartitionClient servicePartitionClient;
#endif

#if !DotNetCoreClr

        internal override DataContractSerializer GetRequestMessageBodySerializer(int interfaceId)
        {
            return this.proxyGeneratorWith.GetRequestMessageBodySerializer(interfaceId);
        }

        internal override DataContractSerializer GetResponseMessageBodySerializer(int interfaceId)
        {
            return this.proxyGeneratorWith.GetResponseMessageBodySerializer(interfaceId);
        }

        internal override object GetResponseMessageBodyValue(object responseMessageBody)
        {
            return ((ActorMessageBody) responseMessageBody).Value;
        }

        internal override object CreateRequestMessageBody(object requestMessageBodyValue)
        {
            return new ActorMessageBody {Value = requestMessageBodyValue};
        }

        internal override Task<byte[]> InvokeAsync(
            int interfaceId,
            int methodId,
            byte[] requestMsgBodyBytes,
            CancellationToken cancellationToken)
        {
            var actorMsgHeaders = new ActorMessageHeaders
            {
                ActorId = this.servicePartitionClient.ActorId,
                InterfaceId = interfaceId,
                MethodId = methodId,
                CallContext = Helper.GetCallContext()
            };

            return this.servicePartitionClient.InvokeAsync(actorMsgHeaders, requestMsgBodyBytes, cancellationToken);
        }

        internal override void Invoke(
            int interfaceId,
            int methodId,
            byte[] requestMsgBodyBytes)
        {
            // actor proxy does not support one way messages
            // actor events are sent from actor event proxy
            throw new NotImplementedException();
        }

        internal void Initialize(
            ActorProxyGeneratorWith actorProxyGeneratorWith,
            Remoting.V1.Client.ActorServicePartitionClient actorServicePartitionClient)
        {
            this.proxyGeneratorWith = actorProxyGeneratorWith;
            this.servicePartitionClient = actorServicePartitionClient;
            this.RemotingClient = RemotingClient.V1Client;
        }
#endif

        #region Event Subscription

        internal async Task SubscribeAsync(Type eventType, object subscriber, TimeSpan resubscriptionInterval)
        {
            if (this.RemotingClient.Equals(RemotingClient.V2Client))
            {
                await this.SubscribeAsyncV2(eventType, subscriber, resubscriptionInterval);
                return;
            }

#if !DotNetCoreClr
            ActorId actorId = this.servicePartitionClient.ActorId;
            SubscriptionInfo info = Remoting.V1.Client.ActorEventSubscriberManager.Singleton.RegisterSubscriber(
                actorId,
                eventType,
                subscriber);

            Exception error = null;
            try
            {
                await this.servicePartitionClient.SubscribeAsync(info.Subscriber.EventId, info.Id);
            }
            catch (Exception e)
            {
                error = e;
            }

            if (error != null)
            {
                try
                {
                    await this.UnsubscribeAsync(eventType, subscriber);
                }
                catch
                {
                    // ignore
                }

                throw error;
            }

            this.ResubscribeAsync(info, resubscriptionInterval);
#endif
        }


        internal async Task UnsubscribeAsync(Type eventType, object subscriber)
        {
            if (this.RemotingClient.Equals(RemotingClient.V2Client))
            {
                await this.UnsubscribeAsyncV2(eventType, subscriber);
                return;
            }
#if !DotNetCoreClr
            ActorId actorId = this.servicePartitionClient.ActorId;
            SubscriptionInfo info;
            if (Remoting.V1.Client.ActorEventSubscriberManager.Singleton.TryUnregisterSubscriber(
                actorId,
                eventType,
                subscriber,
                out info))
            {
                await this.servicePartitionClient.UnsubscribeAsync(info.Subscriber.EventId, info.Id);
            }
#endif
        }


#if !DotNetCoreClr
        private void ResubscribeAsync(SubscriptionInfo info, TimeSpan resubscriptionInterval)
        {
#pragma warning disable 4014
            // ReSharper disable once UnusedVariable
            Task ignore = Task.Run(
                async () =>
#pragma warning restore 4014
                {
                    while (true)
                    {
                        await Task.Delay(resubscriptionInterval);

                        if (!info.IsActive)
                        {
                            break;
                        }

                        try
                        {
                            await
                                this.servicePartitionClient.SubscribeAsync(info.Subscriber.EventId, info.Id)
                                    .ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                });
        }
#endif

        #endregion
    }
}