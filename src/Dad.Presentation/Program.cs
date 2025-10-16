using Npgsql;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddEndpointsApiExplorer();


// 配置数据库连接
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var app = builder.Build();



app.UseHttpsRedirection();

// 获取所有参与单位
app.MapGet("/api/participants", async () =>
{
    var participants = new List<Participant>();
    
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    using var cmd = new NpgsqlCommand("SELECT id, unit_name, participant_count, contact_person, contact_phone FROM participants ORDER BY id DESC", connection);
    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        participants.Add(new Participant
        {
            Id = reader.GetInt32(0),
            UnitName = reader.GetString(1),
            ParticipantCount = reader.GetInt32(2),
            ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3),
            ContactPhone = reader.IsDBNull(4) ? null : reader.GetString(4)
        });
    }
    
    return Results.Ok(participants);
});

// 添加新的参与单位
app.MapPost("/api/participants", async (ParticipantInput input) =>
{
    // 验证输入
    if (string.IsNullOrWhiteSpace(input.UnitName))
        return Results.BadRequest("单位名称不能为空");
    
    if (input.ParticipantCount <= 0)
        return Results.BadRequest("参与人数必须大于0");
    
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    using var cmd = new NpgsqlCommand(
        "INSERT INTO participants (unit_name, participant_count, contact_person, contact_phone) VALUES (@unitName, @participantCount, @contactPerson, @contactPhone) RETURNING id",
        connection);
    
    cmd.Parameters.AddWithValue("unitName", input.UnitName);
    cmd.Parameters.AddWithValue("participantCount", input.ParticipantCount);
    cmd.Parameters.AddWithValue("contactPerson", (object)input.ContactPerson ?? DBNull.Value);
    cmd.Parameters.AddWithValue("contactPhone", (object)input.ContactPhone ?? DBNull.Value);
    
    var id = (int)await cmd.ExecuteScalarAsync();
    
    return Results.Created($"/api/participants/{id}", new { Id = id });
});

app.Run();

// 数据模型
public class Participant
{
    public int Id { get; set; }
    public string UnitName { get; set; }
    public int ParticipantCount { get; set; }
    public string ContactPerson { get; set; }
    public string ContactPhone { get; set; }
}

public class ParticipantInput
{
    [Required]
    public string UnitName { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int ParticipantCount { get; set; }
    
    public string ContactPerson { get; set; }
    
    public string ContactPhone { get; set; }
}
