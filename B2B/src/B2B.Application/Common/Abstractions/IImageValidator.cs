using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Common.Abstractions
{
    public interface IImageValidator
    { /// <summary>
      /// Проверяет что файл — валидное изображение нужного типа.
      /// 
      /// Валидация:
      /// - Content-Type соответствует расширению
      /// - Magic bytes файла соответствуют (защита от подделки MIME)
      /// - Файл не повреждён (можно открыть как изображение)
      /// - Размер в пределах max (5 MB по спеке)
      /// 
      /// Возвращает ValidationResult — успех с метаданными ИЛИ ошибку с кодом.
      /// </summary>
        Task<ImageValidationResult> ValidateAsync(Stream content, string declaredContentType, CancellationToken ct);

    }

    public sealed record ImageValidationResult(bool IsValid,
        string? ErrorCode,
        string? ErrorMessage,
        string? Extension
        );

}
