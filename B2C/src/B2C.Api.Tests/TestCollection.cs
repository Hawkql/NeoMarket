using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Api.Tests
{
    /// <summary>
    /// Маркер для последовательного запуска всех тестов. Тесты разделяют один
    /// Testcontainers Postgres и одну тестовую БД — параллелизм приведёт к
    /// race conditions при seed/чтении общего состояния.
    /// </summary>
    [CollectionDefinition("Sequential", DisableParallelization = true)]
    public sealed class SequentialCollection { }
}
