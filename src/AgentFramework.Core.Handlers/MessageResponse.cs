using System;
using System.IO;

namespace AgentFramework.Core.Handlers
{
    public class MessageResponse : IDisposable
    {
        private readonly BinaryWriter _writer;

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <value>The stream.</value>
        public Stream Stream { get; }

        public MessageResponse()
        {
            Stream = new MemoryStream();
            _writer = new BinaryWriter(Stream);
        }

        /// <summary>
        /// Write the specified data to the response stream.
        /// </summary>
        /// <param name="data">Data.</param>
        public void Write(byte[] data)
        {
            Stream.Seek(0, SeekOrigin.End);
            _writer.Write(data);
        }

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stream.Flush();
                    Stream.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}