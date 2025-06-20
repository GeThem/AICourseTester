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
        public List<ANode> GetFPTrain(int heuristic = 1, int iters = 3, int dimensions = 3)
        {
            var aNode = FifteenPuzzleService.GenerateState(iters, heuristic, dimensions);
            //ANode aNode = new ANode(dimensions);
            //FifteenPuzzleService.ShuffleState(aNode);
            var (_, list) = FifteenPuzzleService.GenerateTree(aNode, iters);
            return list;
        }

        [HttpPost("FifteenPuzzle/Train")]
        public ActionResult<List<ANodeDTO>> PostFPTrainVerify(List<ANode> list, [System.Web.Http.FromUri] int heuristic = 1)
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
        public async Task<ActionResult<FifteenPuzzleDTO>> GetFPTest()
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                return NotFound();
            }
            if (fp.IsSolved)
            {
                var (_, problem) = FifteenPuzzleService.GenerateTree(new ANode() { State = fp.Problem.FromJson<int[][]>() }, fp.TreeHeight);
                var solution = fp.Solution.FromJson<List<ANodeDTO>>();
                var userSolution = fp.UserSolution.FromJson<List<ANodeDTO>>();
                return new FifteenPuzzleDTO() 
                { 
                    Problem = problem, 
                    Solution = solution, 
                    UserSolution = userSolution, 
                    Date = fp.Date,
                    IsSolved = fp.IsSolved,
                    Heuristic = fp.Heuristic,
                    Dimensions = fp.Dimensions,
                    TreeHeight = fp.TreeHeight,
                };
            }
            if (fp.Problem == null)
            {
                var aNode = FifteenPuzzleService.GenerateState(fp.TreeHeight, (int)fp.Heuristic, fp.Dimensions);
                fp.Problem = aNode.State.ToJson();
                _context.Fifteens.Update(fp);
                await _context.SaveChangesAsync();
            }
            var (_, list) = FifteenPuzzleService.GenerateTree(new ANode() { State = fp.Problem.FromJson<int[][]>() }, fp.TreeHeight);
            return new FifteenPuzzleDTO()
            {
                Problem = list,
                Date = fp.Date,
                IsSolved = fp.IsSolved,
                Heuristic = fp.Heuristic,
                Dimensions = fp.Dimensions,
                TreeHeight = fp.TreeHeight,
            };
        }

        [Authorize, HttpPost("FifteenPuzzle/Test")]
        public async Task<ActionResult<List<ANodeDTO>>> PostFPTestVerify(List<ANodeDTO> userSolution)
        {
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                return NotFound();
            }
            if (fp.IsSolved)
            {
                return fp.Solution.FromJson<List<ANodeDTO>>();
            }
            var (problemTree, problem) = FifteenPuzzleService.GenerateTree(new ANode() { State = fp.Problem.FromJson<int[][]>() }, fp.TreeHeight);

            fp.UserSolution = userSolution.ToJson();
            var solution = FifteenPuzzleService.Search(problemTree, FifteenPuzzleService.Heuristics[(int)fp.Heuristic - 1]);
            fp.Solution = solution.ToJson();
            fp.IsSolved = true;

            _context.Fifteens.Update(fp);
            await _context.SaveChangesAsync();
            return solution;
        }

        private async Task<bool> _assignTask(string userId, int heuristic, int dimensions, int iters)
        {
            if ((await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)) == null)
            {
                return false;
            }
            var fp = await _context.Fifteens.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null)
            {
                fp = _context.Fifteens.Add(new FifteenPuzzle() { UserId = userId }).Entity;
                await _context.SaveChangesAsync();
            }
            fp.Heuristic = heuristic;
            fp.Dimensions = dimensions;
            fp.TreeHeight = iters;
            fp.UserSolution = null;
            fp.Solution = null;
            fp.IsSolved = false;
            fp.Date = DateTime.Now;
            
            _context.Fifteens.Update(fp);
            return true;
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("FifteenPuzzle/Users/Assign")]
        public async Task<ActionResult> PostFPTestAssign(string[] userIds, int dimensions = 3, int iters = 3, int heuristic = 1)
        {
            foreach (var userId in userIds)
            {
                await _assignTask(userId, heuristic, dimensions, iters);
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("FifteenPuzzle/Users/{userId}/Assign")]
        public async Task<ActionResult> PostFPTestAssign(string userId, int dimensions = 3, int iters = 3, int heuristic = 1)
        {
            if (await _assignTask(userId, heuristic, dimensions, iters))
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("FifteenPuzzle/Groups/{groupId}/Assign")]
        public async Task<ActionResult> PostFPTestAssign(int groupId, int dimensions = 3, int iters = 3, int heuristic = 1)
        {
            var userIds = await _context.UserGroups.Include(ug => ug.User).Where(ug => ug.GroupId == groupId).Select(ug => ug.UserId).ToArrayAsync();
            foreach (var userId in userIds)
            {
                await _assignTask(userId, heuristic, dimensions, iters);
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
                        Solution = fp.Solution == null ? null : fp.Solution.FromJson<List<ANodeDTO>>(),
                        UserSolution = fp.UserSolution == null ? null : fp.UserSolution.FromJson<List<ANodeDTO>>(),
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
        public async Task<ActionResult> UpdateFPTest(int[][]? State, string userId, [System.Web.Http.FromUri] int? iters = null, [System.Web.Http.FromUri] int? dimensions = null, [System.Web.Http.FromUri] bool generate = false)
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
            if (iters != null)
            {
                fp.TreeHeight = (int)iters;
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
