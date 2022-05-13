using System;
using System.Threading.Tasks;

namespace ModIO.Implementation
{
    internal interface IModIOZipOperation : IDisposable
    {
        Task GetOperation();
        void Cancel();
    }
}
