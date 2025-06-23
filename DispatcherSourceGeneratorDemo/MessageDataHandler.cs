using System.Threading.Tasks;
using SourceGenExperiment.Sample;
using SourceGenExperiment.Sample2;
using SourceGenExperiment.Sample3;

namespace SourceGenExperiment.Sample4
{
    public class MessageDataHandler : IMessageHandler<MessageData>
    {
        public Task HandleMessage(MessageData message)
        {
            //do something
            return Task.CompletedTask;
        }
    }
}

namespace SourceGenExperiment.Sample5
{
    public class MessageDataHandler2 : IMessageHandler<MessageData2>
    {
        public Task HandleMessage(MessageData2 message)
        {
            //do something
            return Task.CompletedTask;
        }
    }
}
