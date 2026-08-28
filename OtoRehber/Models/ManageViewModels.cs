using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut şifre")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifre")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifre (tekrar)")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class ChangeEmailViewModel
    {
        [Required(ErrorMessage = "Yeni e-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
        [Display(Name = "Yeni e-posta adresi")]
        public string NewEmail { get; set; } = "";
    }

    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Hesabınızı silmek için şifrenizi girin.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifreniz")]
        public string Password { get; set; } = "";

        [Range(typeof(bool), "true", "true", ErrorMessage = "Devam etmek için onay kutusunu işaretleyin.")]
        [Display(Name = "Hesabımın ve tüm verilerimin kalıcı olarak silineceğini anlıyorum")]
        public bool Confirm { get; set; }
    }
}
