﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models.TableSchema
{
    public class resetPassword
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [Required]
        public string UserEmail { get; set; }
        [Required]
        public int otp { get; set; }
        public DateTime expiry { get; set; } = DateTime.Now.AddHours(1);
        public int failedTries { get; set; } = 0;
    }
}
