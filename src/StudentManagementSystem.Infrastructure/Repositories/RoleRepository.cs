using Microsoft.Data.SqlClient;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Domain.Entities;
using StudentManagementSystem.Infrastructure.Data;

namespace StudentManagementSystem.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public RoleRepository(DbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<List<Role>> GetAllRolesAsync()
    {
        var roles = new List<Role>();

        using var connection = _connectionFactory.CreateConnection();

        var query = @"
            SELECT
                Id,
                RoleName,
                IsActive,
                CreatedDate,
                CreatedBy,
                ModifiedDate,
                ModifiedBy
            FROM tbl_Roles WITH(NOLOCK)
            WHERE IsActive = 1";

        using var command = new SqlCommand(query, connection);

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            roles.Add(new Role
            {
                Id = Convert.ToInt32(reader["Id"]),
                RoleName = reader["RoleName"].ToString()!,
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                CreatedBy = reader["CreatedBy"].ToString()!,
                ModifiedDate = reader["ModifiedDate"] as DateTime?,
                ModifiedBy = reader["ModifiedBy"].ToString()!,
            });
        }

        return roles;
    }
}
