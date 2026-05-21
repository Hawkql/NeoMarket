using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.FileStorage
{
    /// <summary>
    /// Конфигурация MinIO/S3 storage. Биндится из appsettings.json через Options pattern.
    /// </summary>
    public sealed class MinioOptions
    {
        public string ServiceUrl { get; set; } = null!;     // http://localhost:9000 для dev
        public string AccessKey { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public string BucketName { get; set; } = null!;
        public string PublicUrlBase { get; set; } = null!;  // http://localhost:9000/b2b-images
    }
}
