using System.Threading.Tasks;
using ImageObjectRecognizer.Models;

namespace ImageObjectRecognizer.Writers
{
    public interface IResultWriter
    {
        Task PersistResultAsync(Result result);
    }
}