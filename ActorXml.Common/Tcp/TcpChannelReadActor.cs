using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Proto;

namespace ActorXml.Common.Tcp {
    public class TcpChannelReadActor : IActor
    {
        private readonly TcpClient _client;
        private readonly CancellationTokenSource _cts;
        private Encoding _encoding = Encoding.UTF8;

        public TcpChannelReadActor(TcpClient client, CancellationTokenSource cts) {
            _client = client;
            _cts = cts;
        }

        public async Task ReceiveAsync(IContext context) {
            if (context.Message is Started) {
                XmlReader reader = GetReader(false);
                while (true) {
                    try {
                        while (await reader.ReadAsync()) {
                            if (reader.NodeType == XmlNodeType.Element) {
                                break;
                            }
                        }
                        if (reader.NodeType != XmlNodeType.Element) {
                            break;
                        }
                        var el = XElement.Load(reader.ReadSubtree());
                        Console.WriteLine(
                            $"thread: {Thread.CurrentThread.ManagedThreadId} Received: {el.Name},  Nodetype: {el.NodeType}\r\n {el} ");
                        context.Parent.Request(TcpChannelActor.Messages.MessageRead(el), context.Self);
                    } catch (IOException ioe) when (!_client.Connected) {
                        Console.WriteLine("Devinfo: " + ioe.Message);
                        break;
                    } catch (XmlException ex) {
                        Console.WriteLine("EX: " + ex.Message);
                        reader.Dispose();
                        reader = GetReader(false);
                    }
                }

                context.Parent.Request(TcpChannelActor.Messages.ClientClosed(), context.Self);
            }
        }

        private XmlReader GetReader(bool async) {
            return XmlReader.Create(_client.GetStream(), new XmlReaderSettings {
                    ConformanceLevel = ConformanceLevel.Fragment,
                    CloseInput = false,
                    Async = true,
                    IgnoreComments = true,
                    ValidationType = ValidationType.None
                }
            );
        }
    }







    public class InterceptedStream : Stream {
        public delegate void DataHandlder(byte[] bytes);

        private readonly Stream _baseStream;
        private readonly DataHandlder _readBytes;

        public InterceptedStream(Stream baseStream, DataHandlder readBytes) {
            _baseStream = baseStream;
            _readBytes = readBytes;
        }
        public override void Flush() {
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            _baseStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count) {
            var read = _baseStream.Read(buffer, offset, count);
            //Task.Run(() => 
            _readBytes?.Invoke(buffer.Skip(offset).Take(read).ToArray());
            //);
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count) {
            _baseStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            int read = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (read == 0) {
                _baseStream.Close();
                Close();
            }
            //Task.Run(() => 
            _readBytes?.Invoke(buffer.Skip(offset).Take(read).ToArray());
            //  );
            return read;
        }




    }



}
