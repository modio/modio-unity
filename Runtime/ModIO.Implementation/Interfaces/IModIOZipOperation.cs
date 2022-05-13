using System;
using System.Threading.Tasks;

namespace ModIO.Implementation
{
    internal interface IModIOZipOperation : IDisposable
    {
        Task Operation { get; }
        void Cancel();
    }
}
