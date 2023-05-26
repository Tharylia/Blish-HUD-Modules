namespace Estreya.BlishHUD.Shared.IO;

using System;
using System.IO;

public class ReadProgressStream : Stream
{
    private double _lastProgress;

    public ReadProgressStream(Stream stream)
    {
        this.ContainedStream = stream ?? throw new ArgumentNullException("stream");

        if (stream.Length <= 0 || !stream.CanRead)
        {
            throw new ArgumentException("stream");
        }
    }

    protected Stream ContainedStream { get; }

    public override bool CanRead => this.ContainedStream.CanRead;

    public override bool CanSeek => this.ContainedStream.CanSeek;

    public override bool CanWrite => this.ContainedStream.CanWrite;

    public override long Length => this.ContainedStream.Length;

    public override long Position
    {
        get => this.ContainedStream.Position;
        set => this.ContainedStream.Position = value;
    }

    public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

    public override void Flush() { this.ContainedStream.Flush(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int amountRead = this.ContainedStream.Read(buffer, offset, count);
        if (this.ProgressChanged != null)
        {
            double newProgress = this.Position * 100.0 / this.Length;
            if (newProgress > this._lastProgress)
            {
                this._lastProgress = newProgress;
                this.ProgressChanged(this, new ProgressChangedEventArgs(this._lastProgress));
            }
        }

        return amountRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return this.ContainedStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        this.ContainedStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        this.ContainedStream.Write(buffer, offset, count);
    }

    public class ProgressChangedEventArgs
    {
        public ProgressChangedEventArgs(double progress)
        {
            this.Progress = progress;
        }

        public double Progress { get; private set; }
    }
}