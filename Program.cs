using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySql.Data.MySqlClient;
using Server.Extensions;
using Server.Interfaces;
using System.Reflection;
using Server.Services;
using Server.Utils;
using System.Data;
using System.Text;
using DotNetEnv;


Env.Load();
var connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
var secretKey = Environment.GetEnvironmentVariable("SecretKey")
?? throw new KeyNotFoundException("Secret key not found");
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLazyResolution();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

//? Add scoped services to the container
builder.Services.AddScoped<IDbConnection>(sp => new MySqlConnection(connectionString));
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ISecretKeyService, SecretKeyService>();
builder.Services.AddScoped<IAuthUserService, AuthUserService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<ICampsiteService, CampsiteService>();
builder.Services.AddScoped<IAttributeService, AttributeService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped(sp => {
	var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
	var httpRequest = (httpContextAccessor.HttpContext?.Request)
	?? throw new InvalidOperationException("HttpRequest is not available.");
	return new RequestHelper(httpRequest);
});

//? Define authentication scheme and token validation parameters
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options => {
	options.TokenValidationParameters = new TokenValidationParameters {
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = "MyCamp",
		ValidAudience = "users",
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
	};
});

//? Define authorization policies
builder.Services.AddAuthorization(options => {
	options.AddPolicy("Admin", policy => policy.RequireRole("admin"));
});

//? Define CORS policies
builder.Services.AddCors(options => {
	options.AddPolicy("AllowAllOrigins", builder => {
		builder	.AllowAnyOrigin()
					.AllowAnyMethod()
					.AllowAnyHeader();
	});
});

builder.Services.AddSwaggerGen(options => {
	var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
	options.IncludeXmlComments(xmlPath);
	
	options.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
		Description = "JWT Authorization header using the Bearer scheme",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer"
	});
	options.AddSecurityRequirement(new OpenApiSecurityRequirement {{
		new OpenApiSecurityScheme {
			Reference = new OpenApiReference {
				Type = ReferenceType.SecurityScheme,
				Id = "Bearer"
			}
		},
		Array.Empty<string>()
	}});
});

//? Set up the application
var app = builder.Build();

//? Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI();
	app.MapGet("/", context => Task.Run(
		() => context.Response.Redirect("/swagger")
	));
}

app.UseCors("AllowAllOrigins");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();
