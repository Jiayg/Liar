﻿using Liar.Application.Contracts.Dtos.Sys.User;
using Liar.Application.Contracts.IServices.Sys;
using Liar.Domain.Shared.ConfigModels;
using Liar.HttpApi.Host.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Liar.HttpApi.Host.Controllers
{
    [Route("account")]
    [ApiController]
    [AllowAnonymous]
    public class AccountController : BaseController
    {
        private readonly JwtConfig _jwtConfig;
        private readonly IAccountService _accountService;

        public AccountController(IOptionsSnapshot<JwtConfig> jwtConfig, IAccountService accountService)
        {
            this._jwtConfig = jwtConfig.Value;
            this._accountService = accountService;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<UserTokenInfoDto>> LoginAsync([FromBody] UserLoginDto input)
        {
            var result = await _accountService.LoginAsync(input);

            return new UserTokenInfoDto
            {
                Token = JwtTokenHelper.CreateAccessToken(_jwtConfig, result.Content),
                RefreshToken = JwtTokenHelper.CreateRefreshToken(_jwtConfig, result.Content)
            };
        }

        /// <summary>
        /// 获取个人信息
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<UserInfoDto>> GetCurrentUserInfoAsync([FromRoute] long id)
        {
            return await _accountService.GetUserInfoAsync(id);
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <returns></returns>
        [HttpDelete()]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Logout()
        {
            return NoContent();
        }

        /// <summary>
        /// 刷新token
        /// </summary>
        [HttpPut()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<UserTokenInfoDto>> RefreshAccessTokenAsync([FromBody] UserRefreshTokenDto input)
        {
            var result = await _accountService.GetUserValidateInfoAsync(input.Id);

            if (result == null)
                return Ok(new UserTokenInfoDto
                {
                    Token = JwtTokenHelper.CreateAccessToken(_jwtConfig, result, input.RefreshToken),
                    RefreshToken = input.RefreshToken
                });

            return NotFound();
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPatch("password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> ChangePassword([FromBody] UserChangePwdDto input)
        {
            long id = 0;
            return Result(await _accountService.UpdatePasswordAsync(id, input));
        }
    }
}
