using System.Threading.Tasks;
using ImageMetadataUpdater.Models;

namespace ImageMetadataUpdater.Writers
{
    public interface IResultWriter
    {
        Task PersistResultAsync(Result result);
    }
}