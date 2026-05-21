using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
namespace B2B.Infrastructure.ImageProcessing
{
    /// <summary>
    /// Реализация IImageValidator через библиотеку ImageSharp.
    /// 
    /// Проверки:
    /// 1. Размер файла ≤ 5 МБ
    /// 2. Magic bytes (декодер ImageSharp читает заголовок, отличает реальный JPEG
    ///    от просто переименованного .exe)
    /// 3. Файл не повреждён (Identify извлекает метаданные без полной декомпрессии)
    /// 4. Формат соответствует одному из разрешённых: JPEG, PNG, WebP
    /// </summary>
    public sealed class ImageSharpValidator : IImageValidator
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;  // 5 МБ по спеке

        private readonly ILogger<ImageSharpValidator> _logger;

        public ImageSharpValidator(ILogger<ImageSharpValidator> logger)
        {
            _logger = logger;
        }

        public async Task<ImageValidationResult> ValidateAsync(
            Stream content,
            string declaredContentType,
            CancellationToken ct)
        {
            // 1. Размер
            if (content.Length > MaxFileSizeBytes)
            {
                return new ImageValidationResult(
                    IsValid: false,
                    ErrorCode: "FILE_TOO_LARGE",
                    ErrorMessage: $"File size {content.Length} exceeds 5 MB limit",
                    Extension: null);
            }

            // Сохраняем позицию, чтобы перемотать stream после анализа
            content.Position = 0;

            try
            {
                // 2-3. Декодирование и magic bytes
                // Identify читает только заголовок — быстро, мало памяти.
                // Если файл повреждён или не картинка — бросит UnknownImageFormatException.
                var info = await Image.IdentifyAsync(content, ct);

                // 4. Какой формат?
                string extension = info.Metadata.DecodedImageFormat switch
                {
                    JpegFormat => "jpg",
                    PngFormat => "png",
                    WebpFormat => "webp",
                    _ => "" // неподдерживаемый формат
                };

                if (string.IsNullOrEmpty(extension))
                {
                    return new ImageValidationResult(
                        IsValid: false,
                        ErrorCode: "UNSUPPORTED_MEDIA_TYPE",
                        ErrorMessage:
                            $"Format '{info.Metadata.DecodedImageFormat?.Name ?? "unknown"}' not supported. " +
                            "Allowed: JPEG, PNG, WebP.",
                        Extension: null);
                }

                // Возвращаем stream в начало — Handler передаст его в FileStorage
                content.Position = 0;

                return new ImageValidationResult(
                    IsValid: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    Extension: extension);
            }
            catch (UnknownImageFormatException)
            {
                return new ImageValidationResult(
                    IsValid: false,
                    ErrorCode: "INVALID_IMAGE",
                    ErrorMessage: "File is not a recognizable image",
                    Extension: null);
            }
            catch (InvalidImageContentException)
            {
                return new ImageValidationResult(
                    IsValid: false,
                    ErrorCode: "INVALID_IMAGE",
                    ErrorMessage: "Image is corrupted",
                    Extension: null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during image validation");
                return new ImageValidationResult(
                    IsValid: false,
                    ErrorCode: "INVALID_IMAGE",
                    ErrorMessage: "Image validation failed",
                    Extension: null);
            }
        }
    }
}
