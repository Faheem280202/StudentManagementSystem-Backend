using Microsoft.Data.SqlClient;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Domain.Entities;
using StudentManagementSystem.Domain.Entities.Response;
using StudentManagementSystem.Infrastructure.Data;

namespace StudentManagementSystem.Infrastructure.Repositories;

public class UserRepository(DbConnectionFactory connectionFactory) : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory; 

    private static UserResponse MapUser(SqlDataReader reader)
    {
        return new UserResponse
        {
            UserId = Convert.ToInt32(reader["Id"]),
            UserName = reader["UserName"].ToString()!,
            FirstName = reader["FirstName"].ToString()!,
            LastName = reader["LastName"].ToString()!,
            Email = reader["Email"].ToString()!,
            Phone = reader["Phone"].ToString()!,
            Role = reader["Role"].ToString()!,

            IsActive = Convert.ToBoolean(reader["IsActive"]),
            CreatedOn = Convert.ToDateTime(reader["CreatedOn"]),

            CreatedBy = reader["CreatedBy"] == DBNull.Value
                ? null
                : reader["CreatedBy"].ToString(),

            ModifiedOn = reader["ModifiedOn"] == DBNull.Value
                ? null
                : Convert.ToDateTime(reader["ModifiedOn"]),

            ModifiedBy = reader["ModifiedBy"] == DBNull.Value
                ? null
                : reader["ModifiedBy"].ToString()
        };
    }

    public async Task<UserDashboardResponse> GetDashboardAsync(int currentUserId, string role)
    {
        using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        var dashboard = new UserDashboardResponse();

        if (role == "Admin")
        {
            const string query = @"
            SELECT
                COUNT(*) AS TotalUsers,
                SUM(CASE WHEN RoleId = 1 THEN 1 ELSE 0 END) AS TotalAmins,
                SUM(CASE WHEN RoleId = 2 THEN 1 ELSE 0 END) AS TotalTeachers,
                SUM(CASE WHEN RoleId = 3 THEN 1 ELSE 0 END) AS TotalStudents,
                SUM(CASE WHEN RoleId = 4 THEN 1 ELSE 0 END) AS TotalParents
            FROM tbl_Users
            WHERE IsActive = 1";

            using var command = new SqlCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                dashboard.TotalUsers = Convert.ToInt32(reader["TotalUsers"]);
                dashboard.TotalAdmins = Convert.ToInt32(reader["TotalAmins"]);
                dashboard.TotalTeachers = Convert.ToInt32(reader["TotalTeachers"]);
                dashboard.TotalStudents = Convert.ToInt32(reader["TotalStudents"]);
                dashboard.TotalParents = Convert.ToInt32(reader["TotalParents"]);
            }
        }
        else if (role == "Teacher")
        {
            const string query = @"
                SELECT
                    (SELECT COUNT(*) FROM tbl_Users WHERE IsActive = 1) AS TotalUsers,

                    (SELECT COUNT(*)
                     FROM tbl_Users
                     WHERE RoleId = 2
                       AND IsActive = 1) AS TotalTeachers,

                    COUNT(DISTINCT TS.StudentId) AS TotalStudents,

                    COUNT(DISTINCT PS.ParentId) AS TotalParents

                FROM tbl_TeacherStudents TS
                LEFT JOIN tbl_ParentStudents PS
                    ON TS.StudentId = PS.StudentId
                WHERE TS.TeacherId = @TeacherId";

            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@TeacherId", currentUserId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                dashboard.TotalUsers = Convert.ToInt32(reader["TotalUsers"]);
                dashboard.TotalTeachers = Convert.ToInt32(reader["TotalTeachers"]);
                dashboard.TotalStudents = Convert.ToInt32(reader["TotalStudents"]);
                dashboard.TotalParents = Convert.ToInt32(reader["TotalParents"]);
            }
        }
        else if (role == "Student")
        {
            const string query = @"
                SELECT
                    (
                        SELECT COUNT(*)
                        FROM tbl_Students S
                        WHERE S.ClassId =
                        (
                            SELECT ClassId
                            FROM tbl_Students
                            WHERE UserId = @StudentId
                        )
                    ) AS TotalStudents,

                    (
                        SELECT COUNT(*)
                        FROM tbl_ParentStudents
                        WHERE StudentId = @StudentId
                    ) AS TotalParents";

            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@StudentId", currentUserId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                dashboard.TotalStudents = Convert.ToInt32(reader["TotalStudents"]);
                dashboard.TotalParents = Convert.ToInt32(reader["TotalParents"]);
            }
        }
        else if (role == "Parent")
        {
            const string query = @"
                SELECT
                    (SELECT COUNT(*) FROM tbl_Users WHERE IsActive = 1) AS TotalUsers,

                    COUNT(*) AS TotalStudents

                FROM tbl_ParentStudents
                WHERE ParentId = @ParentId";

            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@ParentId", currentUserId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                dashboard.TotalUsers = Convert.ToInt32(reader["TotalUsers"]);
                dashboard.TotalStudents = Convert.ToInt32(reader["TotalStudents"]);

                dashboard.TotalParents = 0;
                dashboard.TotalTeachers = 0;
            }
        }

        return dashboard;
    }

    public async Task<List<UserResponse>> GetAdminUsersAsync()
    {
        var users = new List<UserResponse>();

        using var connection = _connectionFactory.CreateConnection();

        using var command = new SqlCommand(UserQuery, connection);

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(MapUser(reader));
        }

        return users;
    }

    public async Task<List<UserResponse>> GetTeacherUsersAsync(int teacherId)
    {
        var query = UserQuery + @"
            AND
            (
                U.ID = @TeacherId OR 
                U.Id IN
                (
                    SELECT StudentId
                    FROM tbl_TeacherStudents
                    WHERE TeacherId = @TeacherId
                )

                OR U.Id IN
                (
                    SELECT PS.ParentId
                    FROM tbl_ParentStudents PS
                    INNER JOIN tbl_TeacherStudents TS
                        ON TS.StudentId = PS.StudentId
                    WHERE TS.TeacherId = @TeacherId
                )
            )";

        return await GetUsersAsync(query, "@TeacherId", teacherId);
    }

    public async Task<List<UserResponse>> GetStudentParentsAsync(int studentId)
    {
        var query = UserQuery + @"
            AND U.Id IN
            (
                SELECT ParentId
                FROM tbl_ParentStudents
                WHERE StudentId = @StudentId
            )";

        return await GetUsersAsync(query, "@StudentId", studentId);
    }

    public async Task<List<UserResponse>> GetParentChildrenAsync( int parentId)
    {
        var query = UserQuery + @"
            AND U.Id IN
            (
                SELECT StudentId
                FROM tbl_ParentStudents
                WHERE ParentId = @ParentId
            )";

        return await GetUsersAsync(query, "@ParentId", parentId);
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();

        var query = UserQuery + @"
            AND U.Id = @Id";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Id", id);

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<int> CreateUserAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            const string query = @"
                INSERT INTO tbl_Users
                (
                    UserName,
                    FirstName,
                    LastName,
                    Email,
                    Phone,
                    PasswordHash,
                    RoleId,
                    CreatedBy
                )
                VALUES
                (
                    @UserName,
                    @FirstName,
                    @LastName,
                    @Email,
                    @Phone,
                    @PasswordHash,
                    @RoleId,
                    @CreatedBy
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@UserName", user.UserName);
            command.Parameters.AddWithValue("@FirstName", user.FirstName);
            command.Parameters.AddWithValue("@LastName", user.LastName);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Phone", user.Phone);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@RoleId", user.RoleId);
            command.Parameters.AddWithValue("@CreatedBy", user.CreatedBy);

            var result = await command.ExecuteScalarAsync();
            await transaction.CommitAsync();
            return Convert.ToInt32(result);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CanAccessUserAsync(int currentUserId, string role, int targetUserId)
    {
        switch (role)
        {
            case "Admin":
                return true;

            case "Teacher":
                return await CanTeacherAccessUserAsync(targetUserId);

            case "Student":
                return await CanStudentAccessUserAsync(
                    currentUserId,
                    targetUserId);

            case "Parent":
                return await CanParentAccessUserAsync(
                    currentUserId,
                    targetUserId);

            default:
                return false;
        }
    }

    private async Task<bool> CanTeacherAccessUserAsync(int targetUserId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string query = @"
            SELECT COUNT(1)
            FROM tbl_Users
            WHERE Id = @UserId
              AND RoleId IN (2,3,4)
              AND IsActive = 1";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@UserId",
            targetUserId);

        await connection.OpenAsync();

        var count = await command.ExecuteScalarAsync();

        return Convert.ToInt32(count) > 0;
    }

    private async Task<bool> CanStudentAccessUserAsync(int studentId, int targetUserId)
    {
        if (studentId == targetUserId)
        {
            return true;
        }

        using var connection = _connectionFactory.CreateConnection();

        const string query = @"
            SELECT COUNT(1)
            FROM tbl_ParentStudents
            WHERE StudentId = @StudentId
              AND ParentId = @TargetUserId";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@StudentId",
            studentId);

        command.Parameters.AddWithValue(
            "@TargetUserId",
            targetUserId);

        await connection.OpenAsync();

        var count = await command.ExecuteScalarAsync();

        return Convert.ToInt32(count) > 0;
    }

    private async Task<bool> CanParentAccessUserAsync(int parentId, int targetUserId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string query = @"
            SELECT COUNT(1)
            FROM tbl_ParentStudents
            WHERE ParentId = @ParentId
              AND StudentId = @TargetUserId";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@ParentId",
            parentId);

        command.Parameters.AddWithValue(
            "@TargetUserId",
            targetUserId);

        await connection.OpenAsync();

        var count = await command.ExecuteScalarAsync();

        return Convert.ToInt32(count) > 0;
    }

    public async Task<int> CreateStudentAsync(CreateStudentRequest request, string createdBy)
    {
        using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            // -------------------
            // Student User
            // -------------------

            const string studentQuery = @"
                INSERT INTO tbl_Users
                (
                    UserName,
                    FirstName,
                    LastName,
                    Email,
                    Phone,
                    PasswordHash,
                    RoleId,
                    CreatedBy
                )
                VALUES
                (
                    @UserName,
                    @FirstName,
                    @LastName,
                    @Email,
                    @Phone,
                    @PasswordHash,
                    3,
                    @CreatedBy
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var studentCmd =
                new SqlCommand(
                    studentQuery,
                    connection,
                    transaction);

            studentCmd.Parameters.AddWithValue(
                "@UserName",
                request.StudentUserName);

            studentCmd.Parameters.AddWithValue(
                "@FirstName",
                request.StudentFirstName);

            studentCmd.Parameters.AddWithValue(
                "@LastName",
                request.StudentLastName);

            studentCmd.Parameters.AddWithValue(
                "@Email",
                request.StudentEmail);

            studentCmd.Parameters.AddWithValue(
                "@Phone",
                request.StudentPhone);

            studentCmd.Parameters.AddWithValue(
                "@PasswordHash",
                request.StudentPassword);

            studentCmd.Parameters.AddWithValue(
                "@CreatedBy",
                createdBy);

            int studentId =
                Convert.ToInt32(
                    await studentCmd.ExecuteScalarAsync());

            // -------------------
            // Student Class
            // -------------------

            const string classQuery = @"
                INSERT INTO tbl_Students
                (
                    UserId,
                    ClassId,
                    CreatedBy
                )
                VALUES
                (
                    @StudentId,
                    @ClassId,
                    @CreatedBy
                )";

            using var classCmd =
                new SqlCommand(
                    classQuery,
                    connection,
                    transaction);

            classCmd.Parameters.AddWithValue(
                "@StudentId",
                studentId);

            classCmd.Parameters.AddWithValue(
                "@ClassId",
                request.ClassId);

            classCmd.Parameters.AddWithValue(
                "@CreatedBy",
                createdBy);

            await classCmd.ExecuteNonQueryAsync();

            // -------------------
            // Teacher Mapping
            // -------------------

            const string teacherQuery = @"
                INSERT INTO tbl_TeacherStudents
                (
                    TeacherId,
                    StudentId,
                    CreatedBy
                )
                VALUES
                (
                    @TeacherId,
                    @StudentId,
                    @CreatedBy
                )";

            using var teacherCmd =
                new SqlCommand(
                    teacherQuery,
                    connection,
                    transaction);

            teacherCmd.Parameters.AddWithValue(
                "@TeacherId",
                request.TeacherId);

            teacherCmd.Parameters.AddWithValue(
                "@StudentId",
                studentId);

            teacherCmd.Parameters.AddWithValue(
                "@CreatedBy",
                createdBy);

            await teacherCmd.ExecuteNonQueryAsync();

            // -------------------
            // Parent User
            // -------------------

            const string parentQuery = @"
                INSERT INTO tbl_Users
                (
                    UserName,
                    FirstName,
                    LastName,
                    Email,
                    Phone,
                    PasswordHash,
                    RoleId,
                    CreatedBy
                )
                VALUES
                (
                    @UserName,
                    @FirstName,
                    @LastName,
                    @Email,
                    @Phone,
                    @PasswordHash,
                    4,
                    @CreatedBy
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var parentCmd =
                new SqlCommand(
                    parentQuery,
                    connection,
                    transaction);

            parentCmd.Parameters.AddWithValue(
                "@UserName",
                request.ParentUserName);

            parentCmd.Parameters.AddWithValue(
                "@FirstName",
                request.ParentFirstName);

            parentCmd.Parameters.AddWithValue(
                "@LastName",
                request.ParentLastName);

            parentCmd.Parameters.AddWithValue(
                "@Email",
                request.ParentEmail);

            parentCmd.Parameters.AddWithValue(
                "@Phone",
                request.ParentPhone);

            parentCmd.Parameters.AddWithValue(
                "@PasswordHash",
                request.ParentPassword);

            parentCmd.Parameters.AddWithValue(
                "@CreatedBy",
                createdBy);

            int parentId =
                Convert.ToInt32(
                    await parentCmd.ExecuteScalarAsync());

            // -------------------
            // Parent Mapping
            // -------------------

            const string parentStudentQuery = @"
        INSERT INTO tbl_ParentStudents
        (
            ParentId,
            StudentId,
            CreatedBy
        )
        VALUES
        (
            @ParentId,
            @StudentId,
            @CreatedBy
        )";

            using var parentStudentCmd =
                new SqlCommand(
                    parentStudentQuery,
                    connection,
                    transaction);

            parentStudentCmd.Parameters.AddWithValue(
                "@ParentId",
                parentId);

            parentStudentCmd.Parameters.AddWithValue(
                "@StudentId",
                studentId);

            parentStudentCmd.Parameters.AddWithValue(
                "@CreatedBy",
                createdBy);

            await parentStudentCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            return studentId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<UserResponse>> GetTeachersAsync()
    {
        var teachers = new List<UserResponse>();

        using var connection =
            _connectionFactory.CreateConnection();

        const string query = @"
            SELECT
                U.Id,
                U.UserName,
                U.FirstName,
                U.LastName,
                U.Email,
                U.Phone,
                R.RoleName AS [Role],
                U.IsActive,
                U.CreatedOn,
                U.CreatedBy,
                U.ModifiedOn,
                U.ModifiedBy
            FROM tbl_Users U
            INNER JOIN tbl_Roles R
                ON R.Id = U.RoleId
            WHERE U.RoleId = 2
              AND U.IsActive = 1";

        using var command =
            new SqlCommand(query, connection);

        await connection.OpenAsync();

        using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            teachers.Add(MapUser(reader));
        }

        return teachers;
    }

    private const string UserQuery = @"
        SELECT
            U.Id,
            U.UserName,
            U.FirstName,
            U.LastName,
            U.Email,
            U.Phone,
            R.RoleName AS [Role],
            U.IsActive,
            U.CreatedOn,
            U.CreatedBy,
            U.ModifiedOn,
            U.ModifiedBy
        FROM tbl_Users U WITH(NOLOCK)
        INNER JOIN tbl_Roles R WITH(NOLOCK)
            ON R.Id = U.RoleId
        WHERE U.IsActive = 1
          AND R.IsActive = 1";

    private async Task<List<UserResponse>> GetUsersAsync(string query, string? parameterName = null, int parameterValue = 0)
    {
        var users = new List<UserResponse>();

        using var connection = _connectionFactory.CreateConnection();

        using var command = new SqlCommand(query, connection);

        if (!string.IsNullOrWhiteSpace(parameterName))
        {
            command.Parameters.AddWithValue(parameterName, parameterValue);
        }

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(MapUser(reader));
        }

        return users;
    }
}