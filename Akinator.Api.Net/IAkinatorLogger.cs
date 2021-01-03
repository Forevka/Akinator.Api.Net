using System;
using System.Threading.Tasks;

namespace Akinator.Api.Net
{
    public interface IAkinatorLogger
    {
        Task Information(string info);
        Task Error(Exception ex, string error);
        Task Warning(string warning);
    }
}
