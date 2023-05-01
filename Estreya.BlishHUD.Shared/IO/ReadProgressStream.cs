namespace Estreya.BlishHUD.Shared.IO
{
    using System;
    using System.IO;

    public class ReadProgressStream : Stream
    {
        private double _lastProgress = 0;
        private Stream _stream;

        public ReadProgressStream(Stream stream)
        {
            this._stream = stream ?? throw new ArgumentNullException("stream");

            if (stream.Length <= 0 || !stream.CanRead) throw new ArgumentException("stream");
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        protected Stream ContainedStream => this._stream;

        public override bool CanRead => this._stream.CanRead;

        public override bool CanSeek => this._stream.CanSeek;

        public override bool CanWrite => this._stream.CanWrite;

        public override void Flush() { this._stream.Flush(); }

        public override long Length => this._stream.Length;

        public override long Position
        {
            get => this._stream.Position;
            set => this._stream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int amountRead = this._stream.Read(buffer, offset, count);
            if (ProgressChanged != null)
            {
                double newProgress = this.Position * 100.0 / this.Length;
                if (newProgress > this._lastProgress)
                {
                    this._lastProgress = newProgress;
                    ProgressChanged(this, new ProgressChangedEventArgs(this._lastProgress));
                }
            }

            return amountRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this._stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this._stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this._stream.Write(buffer, offset, count);
        }

        public class ProgressChangedEventArgs
        {
            public double Progress { get; private set; }

            public ProgressChangedEventArgs(double progress)
            {
                this.Progress = progress;
            }
        }
    }
}
