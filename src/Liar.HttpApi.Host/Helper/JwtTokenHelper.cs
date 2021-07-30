﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Liar.Application.Contracts.Dtos.Sys.User;
using Liar.Domain.Shared.ConfigModels;
using Microsoft.IdentityModel.Tokens;

namespace Liar.Liar.HttpApi.Host.Helper
{
    //认证服务器安装：System.IdentityModel.Tokens.Jwt
    //资源服务器安装：Microsoft.AspNetCore.Authentication.JwtBearer
    public enum TokenType
    {
        AccessToken = 1,
        RefreshToken = 2
    }

    public class JwtTokenHelper
    {
        public static string CreateToken(JwtConfig jwtConfig, Claim[] claims, TokenType tokenType)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SymmetricSecurityKey));

            string audience = tokenType.Equals(TokenType.AccessToken) ? jwtConfig.Audience : jwtConfig.RefreshTokenAudience;
            int expires = tokenType.Equals(TokenType.AccessToken) ? jwtConfig.Expire : jwtConfig.RefreshTokenExpire;

            var now = DateTime.Now;

            var token = new JwtSecurityToken(
                issuer: jwtConfig.Issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(expires),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            var jwtAccessTokenToken = new JwtSecurityTokenHandler().WriteToken(token);
            return jwtAccessTokenToken;
        }


        public static string CreateAccessToken(JwtConfig jwtConfig, UserValidateDto user)
        {
            var claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Account),
                new Claim(ClaimTypes.Name, user.Name),
                //new Claim(ClaimTypes.Role, user.RoleIds??"0")
                //new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };
            return CreateToken(jwtConfig, claims, TokenType.AccessToken);
        }

        public static string CreateRefreshToken(JwtConfig jwtConfig, UserValidateDto user)
        {
            var claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Account),
            };
            return CreateToken(jwtConfig, claims, TokenType.RefreshToken);
        }

        public static string CreateAccessToken(JwtConfig jwtConfig, UserValidateDto user, string refreshTokenTxt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(refreshTokenTxt);
            if (token != null)
            {
                var claimAccount = token.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

                if (user != null && user.Account == claimAccount)
                {
                    return CreateAccessToken(jwtConfig, user);
                }
            }
            return string.Empty;
        }
    }
}
