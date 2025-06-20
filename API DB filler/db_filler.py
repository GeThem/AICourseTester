import requests
import random
import aiohttp
import asyncio
from aiohttp import ClientSession
from typing import Sequence, Callable
from yarl import URL
from threading import Thread
from selenium import webdriver
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By
import argparse
import os

parser = argparse.ArgumentParser()
parser.add_argument('-n', '--user-hundreds', default=2, type=int)
parser.add_argument('-l', '--groups-list', default=None, type=str)
parser.add_argument('-dg', '--dont-generate', action='store_true')
parser.add_argument('-c', '--clear-db', action='store_true')
parser.add_argument('-da', '--dont-assign-tasks', action='store_true')
parser.add_argument('-u', '--login', default='admin', type=str)
parser.add_argument('-p', '--password', type=str)
args = parser.parse_args()


if args.password is None:
    args.password = os.environ.get('ADMIN_PASSWORD', None)
    if args.password is None:
        raise Exception("Admin password isn't provided")

if args.login == 'admin':
    login = os.environ.get('ADMIN_USERNAME', None)
    if login is not None:
        args.login = login

USER_HUNDREDS_TO_GENERATE: int = min(args.user_hundreds, 6)  # How many hundreds of users will be generated
group_names = None if args.groups_list is None else args.groups_list.split(' ')

response = requests.post("https://localhost:7169/api/Users/Login",
                         json={"userName": args.login, "password": args.password},
                         verify=False)
login_data = response.json()


def get_names():
    global names
    options = webdriver.ChromeOptions()
    options.add_argument('--headless=new')
    with webdriver.Chrome(options=options) as driver:
        while True:
            driver.get('https://planetcalc.ru/8678/')
            content = WebDriverWait(driver, 3).until(EC.presence_of_element_located((By.ID, 'dialogv661ffc2b08fd1_fio'))).text
            if content != ' ':
                break
        names.append(content)
        # source = driver.page_source
        # names.append(re.search('id="dialogv661ffc2b08fd1_fio".*?>(.*?)</', source, re.RegexFlag.DOTALL).groups()[0])


if not args.dont_generate:
    names = []
    threads = [Thread(target=get_names) for _ in range(USER_HUNDREDS_TO_GENERATE)]
    [thread.start() for thread in threads]
    [thread.join() for thread in threads]
    names = list(map(lambda x: x.split(" "), '\n'.join(names).split('\n')))

async def fetch(method: Callable, url: str | URL, return_response=False, **kwargs):
    async with method(url,
                      headers={'Authorization': f"{login_data['tokenType']} {login_data['accessToken']}",
                               'Content-Type': 'application/json'},
                      ssl=False,
                      **kwargs) as response:
        if not return_response:
            return
        if response.status != 200:
            return
        json = await response.json(content_type='application/json')
        return json


async def post_all_groups(session: ClientSession, groups: Sequence[str]):
    tasks = (fetch(session.post, f"https://localhost:7169/api/Users/Groups?groupName={group_name}") for group_name in groups)
    await asyncio.gather(*tasks)


async def post_all_users(session: ClientSession, names: Sequence[tuple[str, str, str]], group_ids: Sequence[int | str]):
    start = await fetch(session.get, 'https://localhost:7169/api/Users', return_response=True)
    start = 0 if start is None else len(start)
    tasks = (fetch(session.post, "https://localhost:7169/api/Users/Register",
                   json={"userName": f"User{i}", "password": "Abc123-",
                         "secondName": second_name, "name": name, "patronymic": patronymic,
                         "groupId": None if len(group_ids) == 0 else random.choice(group_ids)})
             for i, (second_name, name, patronymic) in enumerate(names, start=start+1))
    await asyncio.gather(*tasks)

async def assign_ab_tasks(session: ClientSession, user_ids: Sequence[str]):
    tasks = (fetch(session.post, f'https://localhost:7169/api/AB/Users/{userId}/Assign'
                                f'?template={random.randint(1, 4)}'
                                f'&max={random.randint(10, 50)}')
             for userId in user_ids)
    await asyncio.gather(*tasks)


async def assign_a_tasks(session: ClientSession, user_ids: Sequence[str]):
    tasks = (fetch(session.post, f'https://localhost:7169/api/A/FifteenPuzzle/Users/{userId}/Assign'
                                f'?dimensions={random.randint(3, 4)}'
                                f'&treeHeight={random.randint(3, 5)}'
                                f'&heuristic={random.randint(1, 2)}')
             for userId in user_ids)
    await asyncio.gather(*tasks)


async def main():
    async with aiohttp.ClientSession() as session:
        if args.clear_db:
            user_ids = await fetch(session.get, 'https://localhost:7169/api/Users', return_response=True)
            if user_ids is not None:
                user_ids = tuple(map(lambda x: x['id'], user_ids))
            await fetch(session.delete, f"https://localhost:7169/api/Users", json=user_ids)
            group_ids = await fetch(session.get, 'https://localhost:7169/api/Users/Groups', return_response=True)
            if group_ids is not None:
                group_ids = tuple(map(lambda x: x['id'], group_ids)) + (None,)
            await fetch(session.delete, f"https://localhost:7169/api/Users/Groups", json=group_ids)
        if not args.dont_generate:
            if group_names is not None:
                await post_all_groups(session, group_names)
            group_ids = await fetch(session.get, 'https://localhost:7169/api/Users/Groups', return_response=True)
            if group_ids is not None:
                group_ids = tuple(map(lambda x: x['id'], group_ids))
            await post_all_users(session, names, group_ids)
        if not args.dont_assign_tasks:
            user_ids = await fetch(session.get, 'https://localhost:7169/api/Users', return_response=True)
            if user_ids is not None:
                user_ids = tuple(map(lambda x: x['id'], user_ids))
            await asyncio.gather(assign_ab_tasks(session, user_ids), assign_a_tasks(session, user_ids))


asyncio.run(main())