using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using System.Text.Json;
using AICourseTester.Models;
using AICourseTester.Services;
using AICourseTester.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.IdentityModel.Tokens;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AICourseTester.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MainDbContext _context;

        public AController(MainDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("FifteenPuzzle/Train")]
        public List<ANode> GetFPTrain([System.Web.Http.FromUri] int depth = 3, [System.Web.Http.FromUri] int dimensions = 4)
        {
            ANode aNode = new ANode(dimensions);
            FifteenPuzzleService.ShuffleState(aNode);
            var (_, list) = FifteenPuzzleService.GenerateTree(aNode, depth);
            return list;
        }

        [HttpPost("FifteenPuzzle/Train")]
        public ActionResult<List<ANode>> PostFPTrainVerify(List<ANode> list, [System.Web.Http.FromUri] int heuristic = 1)
        {
            if (heuristic != 1 && heuristic != 2)
            {
                return BadRequest();
            }
            var tree = FifteenPuzzleService.ListToTree(list);
            FifteenPuzzleService.Search(tree, FifteenPuzzleService.Heuristics[heuristic]);
            return list;
        }

        [Authorize, HttpGet("FifteenPuzzle/Test")]
        public async Task<ActionResult<List<ANode>>> GetFPTest()
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                return NotFound();
            }
            if (fp.Problem == null)
            {
                ANode aNode = new ANode(fp.Dimensions);
                FifteenPuzzleService.ShuffleState(aNode);
                var (tree, list) = FifteenPuzzleService.GenerateTree(aNode, fp.TreeDepth);
                fp.Problem = list.ToJson();
                
                if (fp.Heuristic == null)
                {
                    fp.Heuristic = RandomNumberGenerator.GetInt32(2) + 1;
                }
                fp.Solution = FifteenPuzzleService.GenerateSolution(list, (int)fp.Heuristic - 1).ToJson();
                
                _context.Fifteens.Update(fp);
                await _context.SaveChangesAsync();
                return list;
            }
            return fp.Problem.FromJson<List<ANode>>();
        }

        [Authorize, HttpPost("FifteenPuzzle/Test")]
        public async Task<ActionResult<List<ANode>>> PostFPTestVerify(List<ANode> list)
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                return NotFound();
            }
            var (problem, solution) = (fp.Problem.FromJson<List<ANode>>(), fp.Solution.FromJson<List<ANode>>());
            if (!problem.All(list.Contains))
            {
                return BadRequest();
            }
            if (solution == null)
            {
                if (fp.Heuristic == null)
                {
                    fp.Heuristic = RandomNumberGenerator.GetInt32(2) + 1;
                }
                solution = FifteenPuzzleService.GenerateSolution(list, (int)fp.Heuristic - 1);
                fp.Solution = solution.ToJson();
                
                _context.Fifteens.Update(fp);
            }
            fp.IsSolved = true;
            await _context.SaveChangesAsync();
            return solution;
        }

        [Authorize(Roles = "Administrator"), HttpGet("FifteenPuzzle/Users/")]
        public ActionResult<FifteenPuzzle[]?> GetUsers()
        {
            var fp = _context.Fifteens.Where(f => f.User.Email != "admin@admin.com").ToArray();
            return fp;
        }

        [Authorize(Roles = "Administrator"), HttpGet("FifteenPuzzle/Users/{userId}/")]
        public ActionResult<FifteenPuzzle[]?> GetUser(string userId)
        {
            var fp = _context.Fifteens.Where(f => f.UserId == userId).ToArray();
            return fp;
        }

        [Authorize(Roles = "Administrator"), HttpPut("FifteenPuzzle/Users/{userId}/")]
        public async Task<ActionResult> UpdateFPTest(string userId, [System.Web.Http.FromUri] int? depth = null, [System.Web.Http.FromUri] int? dimensions = null, [System.Web.Http.FromUri] bool generate = false)
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null)
            {
                return NotFound();
            }
            if (depth != null)
            {
                fp.TreeDepth = (int)depth;
            }
            if (dimensions != null)
            {
                fp.Dimensions = (int)dimensions;
            }
            fp.Problem = null;
            fp.Solution = null;
            fp.IsSolved = false;
            if (generate == true)
            {
                ANode aNode = new ANode(fp.Dimensions);
                FifteenPuzzleService.ShuffleState(aNode);
                var (tree, list) = FifteenPuzzleService.GenerateTree(aNode, fp.TreeDepth);
                fp.Problem = list.ToJson();

                if (fp.Heuristic == null)
                {
                    fp.Heuristic = RandomNumberGenerator.GetInt32(2) + 1;
                }
                fp.Solution = FifteenPuzzleService.GenerateSolution(list, (int)fp.Heuristic - 1).ToJson();
            }
            _context.Fifteens.Update(fp);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Administrator"), HttpDelete("FifteenPuzzle/Users/{userId}")]
        public async void DeleteFPTest(string userId)
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null) { return; }
            fp.Problem = null;
            fp.Solution = null;
            fp.IsSolved = false;
            _context.Fifteens.Update(fp);
            _context.SaveChanges();
        }
    }
}
