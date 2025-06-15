using gym_project_business_logic.Model;
using gym_project_business_logic.Model.Domains;
using gym_project_business_logic.Repositories.Interface;
using gym_project_business_logic.Services;
using gym_project_business_logic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace gym_project.Controllers
{
    [Authorize(AuthenticationSchemes = "SomeOtherScheme")]
    [ApiController]
	[Route("[controller]")]
	public class CoachController : ControllerBase
	{
		private MapperConfig _config;
		private IRepository<Coach> _serviceRepository;
		private ICoachService _coachService;
		private ITokenService _tokenService;
		private ILogger<CoachController> _logger;
		private IWebHostEnvironment _environment;

		public CoachController(ICoachService coachService, MapperConfig mapper, ILogger<CoachController> logger, IWebHostEnvironment environment,
					ITokenService tokenService, IRepository<Coach> service)
		{
			this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this._config = mapper ?? throw new ArgumentNullException(nameof(mapper));
			this._serviceRepository = service ?? throw new ArgumentNullException(nameof(service));
			this._environment = environment ?? throw new ArgumentNullException(nameof(environment));
			this._tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
			this._coachService = coachService ?? throw new ArgumentNullException(nameof(coachService));
		}

        [AllowAnonymous]
        [HttpPost("register")]
		public async Task<IActionResult> RegisterController([FromForm] DTOCoach modelDTO)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			Coach coach = this._config.CreateMapper().Map<Coach>(modelDTO);

			string salt = PasswordHelper.GenerateSalt();
			string hashedPassword = PasswordHelper.HashPassword(coach.Password, salt);

			coach.Password = hashedPassword;
			coach.Salt = salt;

			if (!await this._coachService.GetEmail(modelDTO.Email))
			{
				ModelState.AddModelError(nameof(modelDTO.Email), "Этот адрес электронной почты уже зарегистрирован.");
				return BadRequest(ModelState);
			}

			if (!this._coachService.IsValidPhoneNumber(modelDTO.PhoneNumber))
			{
				ModelState.AddModelError(nameof(modelDTO.PhoneNumber), "Неверный формат номера телефона.");
				return BadRequest(ModelState);
			}

			try
			{
				await this._serviceRepository.Add(coach);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Ошибка при сохранении пользователя: {ex}");
				return StatusCode(500, "Произошла ошибка при регистрации пользователя.");
			}


			return Ok(new { Message = "Пользователь успешно зарегистрирован." });
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromForm] DTOLogin modelDTO)
		{
			if (!ModelState.IsValid)
			{
				return ValidationProblem();
			}

			Coach? user = await this._coachService.GetCoach(modelDTO.Login, modelDTO.Password);

			if (user == null)
			{
				this._logger.LogWarning("Неудачная попытка входа: неверный логин или пароль.");

				return Unauthorized(new ProblemDetails
				{
					Title = "Неверные учетные данные",
					Detail = "Логин или пароль указаны неверно",
					Status = StatusCodes.Status401Unauthorized
				});
			}

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.Login),
				new Claim(ClaimTypes.Role, user.Status.ToString())
			};

			var cookieOptions = new CookieOptions
			{
				HttpOnly = true,
				Secure = _environment.IsProduction(),
				SameSite = SameSiteMode.Lax,
				Expires = DateTimeOffset.UtcNow.AddMinutes(30)
			};

			var token = this._tokenService.GenerateToken(claims);

			Response.Cookies.Append("authToken", token, cookieOptions);

			//var userDto = this._config.CreateMapper().Map<Coach>(user);

			return Ok(user);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Coach?>> GetCoach(int id)
		{
			return await this._serviceRepository.GetById(id);
		}

		[HttpGet]
		public async Task<IEnumerable<Coach>> GetCoaches()
		{
			return await this._serviceRepository.Get();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateCoachAsync(int id, [FromBody] DTOCoach? newCoach)
		{
			if (newCoach == null)
			{
				return BadRequest("Модель обновления не может быть null");
			}

			var updated = await this._coachService.UpdateCoachAsync(id, newCoach);
			if (!updated)
			{
				return NotFound($"Coach с Id = {id} не найден");
			}

			return Ok(new { Message = $"Обновление прошло успешно!" });
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteCoachAsync(int id)
		{
			var deleted = await this._serviceRepository.Delete(id);
			if (!deleted)
			{
				return NotFound();
			}

			return Ok(new { Message = $"Удаление прошло успешно" });
		}
	}
}

