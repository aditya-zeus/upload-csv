using Task1.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Workers
RabbitConnection savingToDb = new RabbitConnection("saveToDb");
RabbitConnection fileProcessing = new RabbitConnection(_queue_name: "process");

Thread runApp = new Thread(new ThreadStart(app.Run));
Thread subscribeSavingToDb = new Thread(new ThreadStart(savingToDb.SaveToDb));
Thread subscribeFileProcessing = new Thread(new ThreadStart(fileProcessing.BasicSubscribe));

runApp.Start();
subscribeSavingToDb.Start();
subscribeFileProcessing.Start();