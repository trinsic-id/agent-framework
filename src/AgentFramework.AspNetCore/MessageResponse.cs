using System.IO;

namespace AgentFramework.AspNetCore.Middleware
{
    public class MessageResponse
    {
        private BinaryWriter writer;

        public Stream ResponseStream { get; }

        public MessageResponse()
        {
            ResponseStream = new MemoryStream();
            writer = new BinaryWriter(ResponseStream);
        }

        public void Write(byte[] data)
        {
            ResponseStream.Position = ResponseStream.Seek(0, SeekOrigin.End);
            writer.Write(data);
        }
    }
}