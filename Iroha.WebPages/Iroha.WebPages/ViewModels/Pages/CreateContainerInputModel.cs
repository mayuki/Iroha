using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Iroha.WebPages.ViewModels.Pages
{
    public class CreateContainerInputModel
    {
        public String PagePath { get; set; }

        [Required]
        [CustomValidation(typeof(CreateContainerInputModel), "ValidateContainerName")]
        [StringLength(255)]
        public String ContainerName { get; set; }

        public static ValidationResult ValidateContainerName(object value, ValidationContext validationContext)
        {
            var containerName = value as String;
            if (Regex.IsMatch(containerName, "[*?|:<>\"/\\\\]|[\\p{C}-[ ]]"))
                return new ValidationResult("コンテナ名には * ? | \" < > : / \\ および制御文字を含めることはできません");
            if (Regex.IsMatch(containerName, "^\\.+$"))
                return new ValidationResult("コンテナ名をドットのみにすることはできません");
            
            return ValidationResult.Success;
        }
    }
}
