using System;
using System.Reactive.Linq;
using AliceMQ.ExtensionMethods;
using AliceMQ.MailBox.Interface;
using AliceMQ.MailBox.Message;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace AliceMQ.MailBox.Core
{
    public class CustomMailBox<T>: 
        IMailBox<IMessage>
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IMailBox<BasicDeliverEventArgs> _mailBox;

        public bool IgnoreMissingJsonFields => _jsonSerializerSettings.MissingMemberHandling == MissingMemberHandling.Ignore;

        public CustomMailBox(IMailBox<BasicDeliverEventArgs> mailbox, 
            JsonSerializerSettings jsonSeralizerSettings = null)
        {
            _mailBox = mailbox;
            _jsonSerializerSettings = jsonSeralizerSettings;
        }

        private IMessage ConsumeMessage(BasicDeliverEventArgs e)
        {
            try
            {
                var typedMessage = TypedMessage(Payload(e));

                if (typedMessage == null)
                    throw new Exception("typedMessage is null.");

                return new Ok<T>(typedMessage, e);
            }
            catch (Exception ex)
            {
                return new Error(e, ex);
            }
        }

        private T TypedMessage(string payload)
        {
            return typeof(T) == typeof(string)
                ? (T) (object) payload
                : JsonConvert.DeserializeObject<T>(payload, _jsonSerializerSettings);
        }

        private static string Payload(BasicDeliverEventArgs e)
        {
            return e.BasicProperties.GetEncoding().GetString(e.Body);
        }

        public bool AckRequest(ulong deliveryTag, bool multiple)
        {
            return _mailBox.AckRequest(deliveryTag, multiple);
        }

        public bool NackRequest(ulong deliveryTag, bool multiple, bool requeue)
        {
            return _mailBox.NackRequest(deliveryTag, multiple, requeue);
        }

        public bool IsConfirmable => _mailBox.IsConfirmable;

        public IDisposable Subscribe(IObserver<IMessage> observer) =>
            _mailBox.Select(ConsumeMessage).AsObservable().Subscribe(observer);
    }
}