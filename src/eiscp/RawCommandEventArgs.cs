using System;

namespace eiscp
{
    public class RawCommandEventArgs:EventArgs
    {
        public string Command { get; }

        public RawCommandEventArgs(string command)
        {
            Command = command;
        }
    }
}