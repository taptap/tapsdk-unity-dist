using System.Threading.Tasks;
using TapSDK.Achievement.Internal.Util;
using System;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using TapSDK.Login;
using System.Security.Cryptography;
using System.Collections.Generic;
using TapSDK.Achievement.Internal.Model;
using System.Threading;
using TapSDK.Core;

namespace TapTap.Achievement.Standalone.Internal
{
    public class TapAchievementStore
    {
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private TapAchievementStore()
        {

        }

        public static async Task<string> getFilePath()
        {
            TapTapAccount tapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
            if (tapAccount == null)
            {
                return null;
            }
            string openId = tapAccount.openId;
            if (string.IsNullOrEmpty(openId))
            {
                return null;
            }
            string fileName = EncryptString(openId) + ".json";
            
            string newDirecotryName = "TapAchievement";
            if (TapTapSDK.taptapSdkOptions != null && !string.IsNullOrEmpty(TapTapSDK.taptapSdkOptions.clientId)) {
                newDirecotryName = newDirecotryName + "_" + TapTapSDK.taptapSdkOptions.clientId;
            }
            string newDirectoryPath = Path.Combine(Application.persistentDataPath, newDirecotryName);
            // 兼容旧版本数据
            if(!Directory.Exists(newDirectoryPath)){
                string oldDirectoryPath = Path.Combine(Application.persistentDataPath, "TapAchievement");
                if (Directory.Exists(oldDirectoryPath)) {
                    Directory.Move(oldDirectoryPath, newDirectoryPath);
                }
            }
            string path = Path.Combine(Application.persistentDataPath, newDirecotryName, "localAchievementCache", fileName);
            return path;
        }

        public async static Task<List<TapAchievementStoreBean>> getAll()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await getAllInner();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async static Task SaveAll(List<TapAchievementStoreBean> data)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await SaveAllInner(data);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async static Task Save(TapAchievementStoreBean bean)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                List<TapAchievementStoreBean> currentAll = await getAllInner() ?? new List<TapAchievementStoreBean>();
                currentAll.Add(bean);
                await SaveAllInner(currentAll);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async static Task Delete(string uuid)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                TapAchievementLog.Log("Delete uuid = " + uuid);
                List<TapAchievementStoreBean> currentAll = await getAllInner() ?? new List<TapAchievementStoreBean>();
                currentAll.RemoveAll(x => x.UUID == uuid);
                await SaveAllInner(currentAll);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async static Task<List<TapAchievementStoreBean>> getAllInner()
        {
            string filePath = await getFilePath();
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            if (!File.Exists(filePath))
            {
                return null;
            }
            try
            {
                string text;
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] buffer = new byte[fs.Length];
                    await fs.ReadAsync(buffer, 0, buffer.Length);
                    text = Encoding.UTF8.GetString(buffer);
                }

                List<TapAchievementStoreBean> data;
                try
                {
                    data = JsonConvert.DeserializeObject<List<TapAchievementStoreBean>>(text);
                }
                catch (Exception e)
                {
                    TapAchievementLog.Log(e);
                    // Delete the file if deserialization fails
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception deleteException)
                    {
                        TapAchievementLog.Log($"Failed to delete file: {deleteException}");
                    }

                    return null;
                }

                if (data == null)
                {
                    return null;
                }

                return data;
            }
            catch (Exception e)
            {
                TapAchievementLog.Log(e);
                return null;
            }
        }

        private async static Task SaveAllInner(List<TapAchievementStoreBean> data)
        {
            if (data == null)
            {
                return;
            }

            string text;
            try
            {
                text = JsonConvert.SerializeObject(data);
            }
            catch (Exception e)
            {
                TapAchievementLog.Log(e);
                return;
            }

            string dirPath = Path.GetDirectoryName(await getFilePath());
            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            try
            {
                using (FileStream fs = File.Create(await getFilePath()))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(text);
                    await fs.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception e)
            {
                TapAchievementLog.Log(e);
            }
        }
        private static string EncryptString(string str)
        {
            var md5 = MD5.Create();
            // 将字符串转换成字节数组
            byte[] byteOld = Encoding.UTF8.GetBytes(str);
            // 调用加密方法
            byte[] byteNew = md5.ComputeHash(byteOld);
            // 将加密结果转换为字符串
            var sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                // 将字节转换成16进制表示的字符串，
                sb.Append(b.ToString("x2"));
            }

            // 返回加密的字符串
            return sb.ToString();
        }
    }
}