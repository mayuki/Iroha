using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Iroha.WebPages.ViewModels.Pages
{
    public class CreateContentPageInputModel
    {
        public String PagePath { get; set; }

        [Required]
        [CustomValidation(typeof(CreateContentPageInputModel), "ValidateContentName")]
        [StringLength(255)]
        public String ContentName { get; set; }

        public static ValidationResult ValidateContentName(object value, ValidationContext validationContext)
        {
            var containerName = value as String;
            if (Regex.IsMatch(containerName, "[*?|:<>\"/\\\\]|[\\p{C}-[ ]]"))
                return new ValidationResult("ページ名には * ? | \" < > : / \\ および制御文字を含めることはできません");
            if (Regex.IsMatch(containerName, "^\\.+$"))
                return new ValidationResult("ページ名をドットのみにすることはできません");
            
            return ValidationResult.Success;
        }
    }
}
