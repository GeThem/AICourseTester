using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis;
using AICourseTester.Data;
using AICourseTester.Models;
using AICourseTester.Services;
using Microsoft.AspNetCore.RateLimiting;
using AICourseTester.DTO;

namespace AICourseTester.Controllers
{
    [EnableRateLimiting("token")]
    [Route("api/[controller]")]
    [ApiController]
    public class AController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MainDbContext _context;
        private readonly UsersService _usersService;

        public AController(MainDbContext context, UserManager<ApplicationUser> userManager, UsersService usersService)
        {
            _userManager = userManager;
            _context = context;
            _usersService = usersService;
        }

        [HttpGet("FifteenPuzzle/Train")]
        public List<ANode> GetFPTrain(int heuristic, int height = 3, int dimensions = 4)
        {
            var aNode = FifteenPuzzleService.GenerateState(height, heuristic, dimensions);
            //ANode aNode = new ANode(dimensions);
            //FifteenPuzzleService.ShuffleState(aNode);
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
                return NotFound();
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
                return NotFound();
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
            //if (fp.Heuristic == null)
            //{
            //    fp.Heuristic = RandomNumberGenerator.GetInt32(2) + 1;
            //}
            var solution = FifteenPuzzleService.Search(problemTree, FifteenPuzzleService.Heuristics[(int)fp.Heuristic - 1]);
            fp.Solution = solution.ToJson();
            fp.IsSolved = true;

            _context.Fifteens.Update(fp);
            await _context.SaveChangesAsync();
            return new FifteenPuzzleResponse() { Solution = solution };
        }

        private async Task<bool> _assignTask(string userId, int heuristic, int dimensions, int treeHeight)
        {
            if ((await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)) == null)
            {
                return false;
            }
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null)
            {
                _context.Fifteens.Add(new FifteenPuzzle() { UserId = userId });
                await _context.SaveChangesAsync();
                fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            }
            fp.Heuristic = heuristic;
            fp.Dimensions = dimensions;
            fp.TreeHeight = treeHeight;
            fp.UserSolution = null;
            fp.Solution = null;
            fp.IsSolved = false;
            fp.Date = DateTime.Now;
            
            var aNode = FifteenPuzzleService.GenerateState(treeHeight, heuristic, dimensions);
            var (_, listInner) = FifteenPuzzleService.GenerateTree(aNode, treeHeight);

            fp.Problem = aNode.State.ToJson();
            _context.Fifteens.Update(fp);
            return true;
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("FifteenPuzzle/Users/Assign")]
        public async Task<ActionResult> PostFPTestAssign(string[] userIds, int dimensions = 4, int treeHeight = 3, int heuristic = 1)
        {
            foreach (var userId in userIds)
            {
                await _assignTask(userId, heuristic, dimensions, treeHeight);
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("FifteenPuzzle/Users/{userId}/Assign")]
        public async Task<ActionResult> PostFPTestAssign(string userId, int dimensions = 4, int treeHeight = 3, int heuristic = 1)
        {
            if (await _assignTask(userId, heuristic, dimensions, treeHeight))
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("FifteenPuzzle/Groups/{groupId}/Assign")]
        public async Task<ActionResult> PostFPTestAssign(int groupId, int dimensions = 4, int treeHeight = 3, int heuristic = 1)
        {
            var userIds = await _context.UserGroups.Include(ug => ug.User).Where(ug => ug.GroupId == groupId).Select(ug => ug.UserId).ToArrayAsync();
            foreach (var userId in userIds)
            {
                await _assignTask(userId, heuristic, dimensions, treeHeight);
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpGet("FifteenPuzzle/Users/")]
        public async Task<ActionResult<FifteenPuzzleTaskDTO[]?>> GetUsers()
        {
            var fp = _usersService.UserLeftJoinGroup().Join(_context.Fifteens,
                u => u.Id,
                fp => fp.UserId,
                (u, fp) => new FifteenPuzzleTaskDTO
                    {
                        Task = new FifteenPuzzleDTO
                        {
                            Id = fp.Id,
                            Problem = fp.Problem == null ? null : new List<ANode>() { new ANode() { State = fp.Problem.FromJson<int[][]>() } },
                            Solution = null,
                            UserSolution = null,
                            Heuristic = fp.Heuristic,
                            Dimensions = fp.Dimensions,
                            TreeHeight = fp.TreeHeight,
                            IsSolved = fp.IsSolved,
                            Date = fp.Date,
                        },
                        User = u
                });
            return await fp.ToArrayAsync();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpGet("FifteenPuzzle/Users/{userId}/")]
        public async Task<ActionResult<FifteenPuzzleTaskDTO>> GetUser(string userId)
        {
            var fp = await _usersService.UserLeftJoinGroup(userId).Join(_context.Fifteens,
                u => u.Id,
                fp => fp.UserId,
                (u, fp) => new FifteenPuzzleTaskDTO
                {
                    Task = new FifteenPuzzleDTO
                    {
                        Id = fp.Id,
                        Problem = fp.Problem == null ? null : FifteenPuzzleService.GenerateTree(new ANode() { State = fp.Problem.FromJson<int[][]>() }, fp.TreeHeight).Item2,
                        Solution = fp.Solution == null ? null : fp.Solution.FromJson<List<ANodeModel>>(),
                        UserSolution = fp.UserSolution == null ? null : fp.UserSolution.FromJson<List<ANodeModel>>(),
                        Heuristic = fp.Heuristic,
                        Dimensions = fp.Dimensions,
                        TreeHeight = fp.TreeHeight,
                        IsSolved = fp.IsSolved,
                        Date = fp.Date,
                    },
                    User = u
                }).FirstOrDefaultAsync();
            if (fp != null)
            {
                return fp;
            }
            return NotFound();
        }

        [DisableRateLimiting]
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
            fp.Date = DateTime.Now;
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

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpDelete("FifteenPuzzle/Users/{userId}")]
        public async Task<ActionResult> DeleteFPTest(string userId)
        {
            await _context.Fifteens.Where(f => f.UserId == userId).ExecuteDeleteAsync();
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
