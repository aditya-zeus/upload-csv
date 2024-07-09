using Task1;
using Task1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    {
        options.AddPolicy("ReactPolicy",
            builder =>
            {
                builder.WithOrigins("http://localhost:3000") // Adjust with your React app URL
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
    });


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("ReactPolicy");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Thread runApp = new Thread(new ThreadStart(app.Run));
runApp.Start();

// Workers
RabbitConnection savingToDb = default!;
await RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () => {
    Console.WriteLine("Trying connection to RabbitMq for 'saveToDb'");
    savingToDb = new RabbitConnection(_queue_name: "saveToDb");
    await Task.CompletedTask;
});

RabbitConnection fileProcessing = default!;
await RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () => {
    Console.WriteLine("Trying connection to RabbitMq for 'process'");
    fileProcessing = new RabbitConnection(_queue_name: "process");
    await Task.CompletedTask;
});

// RabbitConnection fileProcessing = new RabbitConnection(_queue_name: "process");

Thread subscribeSavingToDb = new Thread(new ThreadStart(savingToDb.SaveToDb));
Thread subscribeFileProcessing = new Thread(new ThreadStart(fileProcessing.BasicSubscribe));

subscribeSavingToDb.Start();
subscribeFileProcessing.Start();

// Console.WriteLine("Fetching from Mongo");
// Test.test();
// mongodb+srv://aditya:PasswordForUploadCsv@upload-csv.lbp5ki1.mongodb.net/?retryWrites=true&w=majority&appName=Upload-CSV