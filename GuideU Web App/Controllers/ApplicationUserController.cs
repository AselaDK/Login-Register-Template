﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationUserController : ControllerBase
    {
        // we need these for user registration and authentication
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationSettings _appSettings;

        //inject these classes to this controller constructor
        public ApplicationUserController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IOptions<ApplicationSettings> appSettings)
        {
            _userManager = userManager; // check wether a user in given username
            _signInManager = signInManager;
            _appSettings = appSettings.Value;   // injected app settings
        }

        //Web API Method for Registration
        [HttpPost]
        [Route("Register")]
        //POST : /api/ApplicationUser/Register
        public async Task<Object> PostApplicationUser(ApplicationUserModel model)     // this method will pass fullname/pwd/... details
        {
            // this model has details of registering user,
            // so we have to create it as Identity User

            //provide the role manually
            model.Role = "Traveller";

            var applicationUser = new ApplicationUser()
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName
            };

            try
            {
                var result = await _userManager.CreateAsync(applicationUser, model.Password);
                //Add role
                await _userManager.AddToRoleAsync(applicationUser, model.Role);
                return Ok(result);
            }
            catch (Exception)
            {
                throw;
            }

        }

        // Login method
        [HttpPost]
        [Route("Login")]
        //POST : /api/ApplicationUser/Login
        public async Task<IActionResult> Login(LoginModel model)
        {
            // check user is there
            var user = await _userManager.FindByEmailAsync(model.Email);
            // check user with given username & password
            if(user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // set for User Role routes, get Roles assigned for users when login
                var role = await _userManager.GetRolesAsync(user);
                IdentityOptions _optons = new IdentityOptions();

                var tokenDiscriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim("UserID", user.Id.ToString()),    // user claim
                        new Claim(_optons.ClaimsIdentity.RoleClaimType, role.FirstOrDefault())     // role claim
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(5),    // Token will be expired after 5 mins of token generation
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.JWT_Secret)), 
                                                                    SecurityAlgorithms.HmacSha256Signature)
                };
                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.CreateToken(tokenDiscriptor);
                var token = tokenHandler.WriteToken(securityToken);
                return Ok(new { token });
            }
            else
            {
                return BadRequest(new { message = "Username or password is incorrect." });
            }
        }
    }
}