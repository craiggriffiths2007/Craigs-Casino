using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Casino.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserStore<IdentityUser> _userStore;
    private readonly IUserEmailStore<IdentityUser> _emailStore;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        IUserStore<IdentityUser> userStore,
        SignInManager<IdentityUser> signInManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(
            100,
            ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(
            "Password",
            ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/Home/Lobby");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Home/Lobby");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return Page();

        var user = CreateUser();

        await _userStore.SetUserNameAsync(
            user,
            Input.Email,
            CancellationToken.None);

        await _emailStore.SetEmailAsync(
            user,
            Input.Email,
            CancellationToken.None);

        var result =
            await _userManager.CreateAsync(
                user,
                Input.Password);

        if (result.Succeeded)
        {
            var userId =
                await _userManager.GetUserIdAsync(user);

            var code =
                await _userManager.GenerateEmailConfirmationTokenAsync(user);

            code = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(code));

            var callbackUrl =
                Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new
                    {
                        area = "Identity",
                        userId,
                        code,
                        returnUrl
                    },
                    protocol: Request.Scheme)!;

            await _emailSender.SendEmailAsync(
                Input.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return RedirectToPage(
                    "RegisterConfirmation",
                    new
                    {
                        email = Input.Email,
                        returnUrl
                    });
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return Page();
    }

    private IdentityUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<IdentityUser>();
        }
        catch
        {
            throw new InvalidOperationException(
                $"Can't create an instance of '{nameof(IdentityUser)}'.");
        }
    }

    private IUserEmailStore<IdentityUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException(
                "The default UI requires a user store with email support.");
        }

        return (IUserEmailStore<IdentityUser>)_userStore;
    }
}
