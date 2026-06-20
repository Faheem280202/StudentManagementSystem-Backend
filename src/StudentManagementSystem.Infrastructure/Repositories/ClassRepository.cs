using Microsoft.Data.SqlClient;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Domain.Entities.Response;
using StudentManagementSystem.Infrastructure.Data;

namespace StudentManagementSystem.Infrastructure.Repositories;

public class ClassRepository(DbConnectionFactory connectionFactory) : IClassRepository
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<List<ClassResponse>> GetClassesAsync()
    {
        var classes = new List<ClassResponse>();

        using var connection = _connectionFactory.CreateConnection();

        const string query = @"
            SELECT
                Id,
                ClassName
            FROM tbl_Classes
            WHERE IsActive = 1
            ORDER BY ClassName";

        using var command = new SqlCommand(query, connection);

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            classes.Add(new ClassResponse
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ClassName = reader.GetString(reader.GetOrdinal("ClassName"))
            });
        }

        return classes;
    }
}