using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using AICourseTester.Data;
using AICourseTester.Models;
using AICourseTester.Services;

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
        public List<ANode> GetFPTrain([System.Web.Http.FromUri] int height = 3, [System.Web.Http.FromUri] int dimensions = 4)
        {
            ANode aNode = new ANode(dimensions);
            FifteenPuzzleService.ShuffleState(aNode);
            var (_, list) = FifteenPuzzleService.GenerateTree(aNode, height);
            return list;
        }

        [HttpPost("FifteenPuzzle/Train")]
        public ActionResult<List<ANodeModel>> PostFPTrainVerify(List<ANode> list, [System.Web.Http.FromUri] int heuristic = 1)
        {
            if (heuristic != 1 && heuristic != 2)
            {
                return BadRequest();
            }
            var tree = FifteenPuzzleService.ListToTree(list);
            var solution = FifteenPuzzleService.Search(tree, FifteenPuzzleService.Heuristics[heuristic]);
            return solution;
        }

        [Authorize, HttpGet("FifteenPuzzle/Test")]
        public async Task<ActionResult<FifteenPuzzleResponse>> GetFPTest()
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                _context.Fifteens.Add(new FifteenPuzzle() { UserId = _userManager.GetUserId(User) });
                _context.SaveChanges();
                fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            }
            if (fp.IsSolved)
            {
                var (_, problem) = FifteenPuzzleService.GenerateTree(new ANode() { State = fp.Problem.FromJson<int[][]>() }, fp.TreeHeight);
                var solution = fp.Solution.FromJson<List<ANodeModel>>();
                var userSolution = fp.UserSolution.FromJson<List<ANodeModel>>();
                return new FifteenPuzzleResponse() { Problem = problem, Solution = solution, UserSolution = userSolution };
            }
            if (fp.Problem == null)
            {
                ANode aNode = new ANode(fp.Dimensions);
                FifteenPuzzleService.ShuffleState(aNode);
                var (_, listInner) = FifteenPuzzleService.GenerateTree(aNode, fp.TreeHeight);

                fp.Problem = aNode.State.ToJson();
                _context.Fifteens.Update(fp);
                await _context.SaveChangesAsync();
                return new FifteenPuzzleResponse() { Problem = listInner };
            }
            var (_, list) = FifteenPuzzleService.GenerateTree(new ANode() { State = fp.Problem.FromJson<int[][]>() }, fp.TreeHeight);
            return new FifteenPuzzleResponse() { Problem = list };
        }

        [Authorize, HttpPost("FifteenPuzzle/Test")]
        public async Task<ActionResult<FifteenPuzzleResponse>> PostFPTestVerify(List<ANodeModel> userSolution)
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                return NotFound();
            }
            if (fp.IsSolved)
            {
                return new FifteenPuzzleResponse() { Solution = fp.Solution.FromJson<List<ANodeModel>>() };
            }
            var (problemTree, problem) = FifteenPuzzleService.GenerateTree(new ANode() { State = fp.Problem.FromJson<int[][]>() }, fp.TreeHeight);

            fp.UserSolution = userSolution.ToJson();
            if (fp.Heuristic == null)
            {
                fp.Heuristic = RandomNumberGenerator.GetInt32(2) + 1;
            }
            var solution = FifteenPuzzleService.Search(problemTree, FifteenPuzzleService.Heuristics[(int)fp.Heuristic - 1]);
            fp.Solution = solution.ToJson();
            fp.IsSolved = true;

            _context.Fifteens.Update(fp);
            await _context.SaveChangesAsync();
            return new FifteenPuzzleResponse() { Solution = solution };
        }

        [Authorize(Roles = "Administrator"), HttpGet("FifteenPuzzle/Users/")]
        public ActionResult<FifteenPuzzle[]?> GetUsers()
        {
            var fp = _context.Fifteens.Where(f => f.User.Email != "admin@admin.com").ToArray();
            return fp;
        }

        [Authorize(Roles = "Administrator"), HttpGet("FifteenPuzzle/Users/{userId}/")]
        public ActionResult<FifteenPuzzleResponse> GetUser(string userId)
        {
            var fp = _context.Fifteens.Where(f => f.UserId == userId).FirstOrDefault();
            if (fp != null)
            {
                var (_, list) = FifteenPuzzleService.GenerateTree(new ANode() { State = fp.Problem.FromJson<int[][]>() }, fp.TreeHeight);
                return new FifteenPuzzleResponse() { Problem = list, Solution = fp.Solution?.FromJson<List<ANodeModel>>(), UserSolution = fp.UserSolution?.FromJson<List<ANodeModel>>() };
            }
            return NotFound();
        }

        [Authorize(Roles = "Administrator"), HttpPut("FifteenPuzzle/Users/{userId}/")]
        public async Task<ActionResult> UpdateFPTest(int[][]? State, string userId, [System.Web.Http.FromUri] int? height = null, [System.Web.Http.FromUri] int? dimensions = null, [System.Web.Http.FromUri] bool generate = false)
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null)
            {
                if (_context.Users.FirstOrDefault(f => f.Id == userId) != null)
                {
                    _context.Fifteens.Add(new FifteenPuzzle() { UserId = userId });
                    _context.SaveChanges();
                    fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == userId);
                }
                else
                {
                    return NotFound();
                }
            }
            if (height != null)
            {
                fp.TreeHeight = (int)height;
            }
            if (dimensions != null)
            {
                fp.Dimensions = (int)dimensions;
            }
            fp.Problem = null;
            fp.Solution = null;
            fp.IsSolved = false;
            if (State != null)
            {
                fp.Problem = State.ToJson();
                fp.Dimensions = State.Length;
            }
            else if (generate == true)
            {
                ANode aNode = new ANode(fp.Dimensions);
                FifteenPuzzleService.ShuffleState(aNode);
                fp.Problem = aNode.State.ToJson();
            }
            _context.Fifteens.Update(fp);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Administrator"), HttpDelete("FifteenPuzzle/Users/{userId}")]
        public async Task<ActionResult> DeleteFPTest(string userId)
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null) { return NotFound(); }
            fp.Problem = null;
            fp.Solution = null;
            fp.UserSolution = null;
            fp.IsSolved = false;
            _context.Fifteens.Update(fp);
            _context.SaveChanges();
            return Ok();
        }
    }
}
