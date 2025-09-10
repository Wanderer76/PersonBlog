using Microsoft.EntityFrameworkCore;
using Npgsql;
using Search.Domain.Entities;
using Shared.Persistence;

namespace Search.Persistence.Repositories
{
    public class NpgSqlSearchRepository : DefaultRepository<SearchDbContext, ISearch>
    {
        private readonly SearchDbContext _context;

        public NpgSqlSearchRepository(SearchDbContext context, IReadRepository<ISearch> readRepository, IWriteRepository<ISearch> writeRepository) 
            : base(readRepository, writeRepository)
        {
            _context = context;
        }

        public async Task<IEnumerable<PostIndex>> SearchPosts(IEnumerable<string> words, int page, int size)
        {
            // Защита от невалидных параметров
            var safeSize = Math.Min(Math.Max(size, 1), 100); // от 1 до 100
            var safePage = Math.Max(page, 1);
            var offset = (safePage - 1) * safeSize;

            // Генерация параметров для слов
            var wordParams = string.Join(", ", words.Select((w, i) => $"@word{i}"));
            var parameters = words
                .Select((w, i) => new NpgsqlParameter($"word{i}", w))
                .ToList();

            // Добавляем параметры пагинации
            parameters.Add(new NpgsqlParameter("offset_param", offset));
            parameters.Add(new NpgsqlParameter("limit_param", safeSize));

            var sql = $@"
SET search_path TO ""Search"", public;
WITH search_words AS (
    SELECT unnest(ARRAY[{wordParams}]::text[]) AS word
),
title_matches AS (
    SELECT
        p.""Id"",
        p.""BlogId"",
        p.""CreatedAt"",
        p.""Title"",
        p.""Description"",
        p.""ViewCount"",
        5.0 * MAX(similarity(p.""Title"", sw.word)) AS score
    FROM ""Search"".""PostIndices"" p
    JOIN search_words sw ON p.""Title"" % sw.word
    GROUP BY p.""Id"", p.""BlogId"", p.""CreatedAt"", p.""Title"", p.""Description"", p.""ViewCount""
),
keyword_matches AS (
    SELECT
        p.""Id"",
        p.""BlogId"",
        p.""CreatedAt"",
        p.""Title"",
        p.""Description"",
        p.""ViewCount"",
        SUM(similarity(pk.""Word"", sw.word) * pk.""Score"") AS score
    FROM ""Search"".""PostIndices"" p
    JOIN ""Search"".""WordScores"" pk ON p.""Id"" = pk.""PostIndexId""
    JOIN search_words sw ON pk.""Word"" % sw.word
    GROUP BY p.""Id"", p.""BlogId"", p.""CreatedAt"", p.""Title"", p.""Description"", p.""ViewCount""
),
combined AS (
    SELECT * FROM title_matches
    UNION ALL
    SELECT * FROM keyword_matches
),
ranked AS (
    SELECT
        ""Id"",
        ""BlogId"",
        ""CreatedAt"",
        ""Title"",
        ""Description"",
        ""ViewCount"",
        SUM(score) AS total_score
    FROM combined
    GROUP BY ""Id"", ""BlogId"", ""CreatedAt"", ""Title"", ""Description"", ""ViewCount""
),
paginated AS (
    SELECT
        ""Id"",
        ""BlogId"",
        ""CreatedAt"",
        ""Title"",
        ""Description"",
        ""ViewCount"",
        total_score,
        ROW_NUMBER() OVER (
            ORDER BY total_score DESC, ""ViewCount"" DESC, ""CreatedAt"" DESC
        ) AS row_num
    FROM ranked
)
SELECT
    ""Id"",
    ""BlogId"",
    ""CreatedAt"",
    ""Title"",
    ""Description"",
    ""ViewCount""
FROM paginated
WHERE row_num > @offset_param
ORDER BY total_score DESC, ""ViewCount"" DESC, ""CreatedAt"" DESC
LIMIT @limit_param;
";

            // Выполняем запрос через EF Core
            return await _context.PostIndices
                .FromSqlRaw(sql, parameters.ToArray())
                .ToListAsync();
        }
    }
}
