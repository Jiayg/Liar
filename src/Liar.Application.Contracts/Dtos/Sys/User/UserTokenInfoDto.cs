﻿using Liar.Domain.Shared.Dtos;

namespace Liar.Application.Contracts.Dtos.Sys.User
{
    public class UserTokenInfoDto : IDto
    {
        /// <summary>
        /// 访问Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 刷新Token
        /// </summary>
        public string RefreshToken { get; set; }
    }
}
