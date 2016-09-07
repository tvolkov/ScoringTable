﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ScoringTable.Models.DAL;
using ScoringTable.Models.DAL.SolvedMazes;

namespace ScoringTable.Models
{
    public class ScoringService
    {
        private readonly string mazeGameApiUrl = "http://192.168.0.50:12345/";
        private readonly string dbApiUrl = "http://192.168.0.50:8080/";

        public List<Team> GetAllTeams()
        {
            var teams = new List<Team>();
            var result = getDataFromRestAPI(dbApiUrl, "restapi/teams").Result;

            var dalTeams = JsonConvert.DeserializeObject<DALTeams>(result);

            var maxScore = int.MinValue;
            var minScore = int.MaxValue;

            foreach (var dalTeam in dalTeams._embedded.teams)
            {
                var team = new Team {Id = dalTeam.externalId, Name = dalTeam.description};
                //getting scores for teams
                var mazes = GetTeam(team.Id);
                foreach (var maze in mazes)
                {
                    var tempMaze = new Maze {Id = maze.mazeId, Score = maze.score};

                    if(maze.score <= minScore && maze.score != 0)
                    {
                        minScore = maze.score;
                        tempMaze.BestTeam = true;
                        teams.ForEach(t =>
                        {
                            if (t.Mazes.Any())
                            {
                                var innerMaze = t.Mazes.FirstOrDefault(m => m.Id == maze.mazeId);
                                if(innerMaze != null && innerMaze.Score != maze.score)
                                    innerMaze.BestTeam = false;
                            }
                        });
                    }
                    else if (maze.score >= maxScore && maze.score != 0)
                    {
                        maxScore = maze.score;
                        tempMaze.WorstTeam = true;
                        teams.ForEach(t =>
                        {
                            if (t.Mazes.Any())
                            {
                                var innerMaze = t.Mazes.FirstOrDefault(m => m.Id == maze.mazeId);
                                if (innerMaze != null && innerMaze.Score != maze.score)
                                    innerMaze.WorstTeam = false;
                            }
                        });
                    }

                    team.Mazes.Add(tempMaze);
                }
                teams.Add(team);
            }

            var solvedMazes = GetSolvedMazes();
            foreach (var solvedMaze in solvedMazes)
            {
                var team = teams.Single(t => t.Id == solvedMaze.teamId);
                if (team.Mazes.Any())
                {
                    var maze = team.Mazes.FirstOrDefault(m => m.Id == solvedMaze.mazeId);
                    if (maze != null)
                        maze.Solved = true;
                }
            }

            return teams.OrderBy(t => t.Sum).ToList();
        }

        public List<SolvedMaze> GetSolvedMazes()
        {
            var result = getDataFromRestAPI(dbApiUrl, "restapi/solvedMazes").Result;

            var dalSolvedMazes = JsonConvert.DeserializeObject<SolvedMazes>(result);

            return dalSolvedMazes._embedded.solvedMazes;
        }

        public List<DAL.Maze> GetTeam(string id)
        {
            var result = getDataFromRestAPI(mazeGameApiUrl, $"maze-game/points/teamMazes/{id}").Result;
            var scores = JsonConvert.DeserializeObject<Scores>(result);
            //r1_1 r1_2
            if (!scores.mazes.Exists(m => m.mazeId == "r1_1"))
            {
                var maze = new DAL.Maze {mazeId = "r1_1"};
                if (id == "1df0455f")
                {
                    maze.score = 16143;
                }
                if (id == "qe78gh03")
                {
                    maze.score = 134;
                }
                if (id == "bvc234a6")
                {
                    maze.score = 140;
                }
                if (id == "kgruh240")
                {
                    maze.score = 50;
                }
                if (id == "06hdbll3")
                {
                    maze.score = 0;
                }
                if (id == "nek436jr")
                {
                    maze.score = 0;
                }

                scores.mazes.Insert(0, maze);
            }

            if (!scores.mazes.Exists(m => m.mazeId == "round2"))
            {
                var maze = new DAL.Maze {mazeId = "round2", score = 0};
                scores.mazes.Insert(2, maze);
            }
            else
            {
                scores.mazes.Reverse(2, 2);
            }

            if (scores.mazes.Exists(m => m.mazeId == "r1_2") && id == "qe78gh03")
            {
                scores.mazes.FirstOrDefault(m => m.mazeId == "r1_2").score-= 5554;
            }

            return scores.mazes;
        }

        public int GetTimeLeft(string timerId)
        {
            var result = getDataFromRestAPI(mazeGameApiUrl, $"maze-game/timer/getRemainingTime/{timerId}").Result;
            var timer = JsonConvert.DeserializeObject<Timer>(result);
            return timer?.remainingTime ?? 1;
        }

        private static async Task<string> getDataFromRestAPI(string baseAddress, string path)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    return data;
                }
            }
            return String.Empty;
        }

        private static async Task<string> postDataToRestAPI(string baseAddress, string path, object json)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var jsonInString = JsonConvert.SerializeObject(json);

                // HTTP POST
                HttpResponseMessage response = await client.PostAsync("api/products", new StringContent(jsonInString, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    Uri jsonUrl = response.Headers.Location;

                    //// HTTP PUT
                    //gizmo.Price = 80;   // Update price
                    //response = await client.PutAsync(jsonUrl, json);

                    //// HTTP DELETE
                    //response = await client.DeleteAsync(gizmoUrl);
                }
            }
            return String.Empty;
        }
    }
}
