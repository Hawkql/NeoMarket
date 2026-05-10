using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Common.Abstractions
{
    /// <summary>
    /// Абстракция файлового хранилища (Hexagonal Architecture port).
    /// 
    /// Реализации:
    /// - LocalFileStorage (Infrastructure) — MEDIA_ROOT для dev
    /// - S3FileStorage / MinIoFileStorage (Infrastructure) — для prod
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Загрузка файла. Возвращает URL для записи в Image.Url.
        /// </summary>
        /// <param name="content">Содержимое файла (валидация уже произведена).</param>
        /// <param name="extension">Расширение без точки ("jpg", "png", "webp").</param>
        /// <param name="folder">Логический "путь" вида "products/{entity_id}".</param>
        Task<string> Upload(string content,string extension, string folder,CancellationToken ct);

        Task DeletedAt(string url,CancellationToken ct);

    }
}
