using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(ITokenService tokenService, IMapper mapper, UserManager<AppUser> userManager) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");

        
        using var hmac = new HMACSHA512();

        var user = mapper.Map<AppUser>(registerDto);

        user.UserName = registerDto.Username.ToLower();
        // user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
        // user.PasswordSalt = hmac.Key;

        // context.Users.Add(user);
        // await context.SaveChangesAsync();

        var result = await userManager.CreateAsync(user, registerDto.Password);

        if(!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new UserDto
        {
            Username = user.UserName,
            Token = await tokenService.CreateToken(user),
            KnownAs = user.KnownAs,
            Gender = user.Gender
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await userManager.Users
            .Include(u => u.Photos)
                .FirstOrDefaultAsync(user => user.NormalizedUserName == loginDto.Username.ToUpper());

        if(user == null || user.UserName == null) return Unauthorized("Invalid username");

        // var hmac = new HMACSHA512(user.PasswordSalt);
        // var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        // if(computedHash.Length != user.PasswordHash.Length) return Unauthorized("Invalid password");
        
        // for(int i=0; i < computedHash.Length; i++)
        // {
        //     if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
        // }

        var result = await userManager.CheckPasswordAsync(user, loginDto.Password);
        
        if(!result) return Unauthorized();

        return Ok(new UserDto
        {
            Username = user.UserName,
            Token = await tokenService.CreateToken(user),
            PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
            KnownAs = user.KnownAs,
            Gender = user.Gender
        }
        );
    }

    private async Task<bool> UserExists(string username)
    {
        return await userManager.Users.AnyAsync(x => x.NormalizedUserName == username.ToUpper());
    }
}
