using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.S3.Model;

namespace B2B.Infrastructure.FileStorage
{
    /// <summary>
    /// Реализация IFileStorage поверх S3-совместимого хранилища (MinIO в dev, AWS S3 в prod).
    /// 
    /// Используется AWS SDK — код переносим: MinIO и реальный S3 говорят по одному протоколу,
    /// меняется только endpoint URL и credentials.
    /// 
    /// Структура ключей: {folder}/{guid}.{extension}
    /// Например: products/abc-123/file-uuid.jpg
    /// </summary>
    public sealed class MinioFileStorage : IFileStorage
    {
        private readonly IAmazonS3 _s3Client;
        private readonly MinioOptions _options;
        private readonly ILogger<MinioFileStorage> _logger;

        public MinioFileStorage(
            IAmazonS3 s3Client,
            IOptions<MinioOptions> options,
            ILogger<MinioFileStorage> logger)
        {
            _s3Client = s3Client;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> UploadAsync(
            Stream content,
            string extension,
            string folder,
            CancellationToken ct)
        {
            // Генерируем уникальное имя файла — Guid защищает от path traversal
            // и коллизий имён.
            var fileName = $"{Guid.NewGuid()}.{extension}";
            var key = $"{folder.Trim('/')}/{fileName}";

            var contentType = extension switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false   // мы не владеем stream'ом, не закрываем
            };

            await _s3Client.PutObjectAsync(request, ct);

            // Возвращаем публичный URL — он попадёт в Image.Url
            var url = $"{_options.PublicUrlBase.TrimEnd('/')}/{key}";

            _logger.LogInformation("Uploaded file to {Key} ({Size} bytes)",
                key, content.Length);

            return url;
        }

        public async Task DeleteAsync(string url, CancellationToken ct)
        {
            // Извлекаем key из публичного URL.
            // PublicUrlBase = "http://localhost:9000/b2b-images"
            // url           = "http://localhost:9000/b2b-images/products/abc/file.jpg"
            // key           = "products/abc/file.jpg"
            var prefix = _options.PublicUrlBase.TrimEnd('/') + "/";

            if (!url.StartsWith(prefix))
            {
                _logger.LogWarning(
                    "Cannot delete file: URL '{Url}' doesn't match storage base '{Prefix}'",
                    url, prefix);
                return;  // идемпотентность — не падаем
            }

            var key = url[prefix.Length..];

            try
            {
                await _s3Client.DeleteObjectAsync(_options.BucketName, key, ct);
                _logger.LogInformation("Deleted file {Key}", key);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Файла уже нет — это нормально (идемпотентность)
                _logger.LogDebug("File {Key} not found in storage (already deleted)", key);
            }
        }
    }
}
