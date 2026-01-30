using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Threading;

namespace RefaccionariaWeb.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Correo")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // === MAGIA: ASIGNAR ROL CLIENTE ===
                    if (!await _roleManager.RoleExistsAsync("Cliente"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Cliente"));
                    }
                    await _userManager.AddToRoleAsync(user, "Cliente");

                    // Auto-confirmar email para que no pida verificar
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    await _userManager.ConfirmEmailAsync(user, token);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return Page();
        }
    }
}