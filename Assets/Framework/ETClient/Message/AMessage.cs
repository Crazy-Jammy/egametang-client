using Core.WindJson;
using Core.Serializer;
using Google.Protobuf;

// 不要在这个文件加[ProtoInclude]跟[BsonKnowType]标签,加到InnerMessage.cs或者OuterMessage.cs里面去
namespace Model
{
    [SBGroupInerited("Protocol")]
    public abstract partial class AMessage : SerializerBinary
    {
        public override string ToString()
        {
            var rJsonNode = JsonParser.ToJsonNode(this);
            return rJsonNode?.ToString();
        }
    }

    public abstract partial class ARequest : AMessage
    {
        public int RpcId;
    }

    /// <summary>
    /// 服务端回的RPC消息需要继承这个抽象类
    /// </summary>
    public abstract partial class AResponse : AMessage
    {
        public int RpcId;
        public int Error = 0;
        public string Message = "";
    }

    public interface IMessage : Google.Protobuf.IMessage
    {
    }

    public interface IRequest : IMessage
    {
        int RpcId { get; set; }
    }

    public interface IResponse : IMessage
    {
        int Error { get; set; }
        string Message { get; set; }
        int RpcId { get; set; }
    }

    public class ResponseMessage : IResponse
    {
        public int Error { get; set; }
        public string Message { get; set; }
        public int RpcId { get; set; }

        public void MergeFrom(CodedInputStream input)
        {
            throw new System.NotImplementedException();
        }

        public void WriteTo(CodedOutputStream output)
        {
            throw new System.NotImplementedException();
        }

        public int CalculateSize()
        {
            throw new System.NotImplementedException();
        }
    }
}