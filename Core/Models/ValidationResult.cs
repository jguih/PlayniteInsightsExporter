using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class ValidationResult
    {
        private int? _httpCode;
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public int? HttpCode
        {
            get => _httpCode ?? (IsValid ? 200 : 400);
            set => _httpCode = value;
        }

        public ValidationResult(bool IsValid, string Message, int? HttpCode)
        {
            this.IsValid = IsValid;
            this.Message = Message;
            this.HttpCode = HttpCode;
        }
    }
}
