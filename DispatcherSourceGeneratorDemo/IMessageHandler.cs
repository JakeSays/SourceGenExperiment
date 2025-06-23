using System.Threading.Tasks;


namespace SourceGenExperiment.Sample3;

public interface IMessageHandler<in TType>
{
    Task HandleMessage(TType message);
}
