﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace RMP.App.Common
{
    public class WebHelpers
    {
        /// <summary>
        /// Creates a GET request.
        /// </summary>
        /// <param name="url">URL that's gonna get the request.</param>
        /// <returns>Response as a string.</returns>
        public static async Task<string> CreateGETRequestAsync(string url)
        {
            HttpClient client = new HttpClient();
            HttpRequestHeaderCollection headers = client.DefaultRequestHeaders;

            string httpResponseBody = null;

            string header = URLs.UserAgent;
            if (!headers.UserAgent.TryParseAdd(header))
            {
                throw new Exception("Invalid header value: " + header);
            }

            // Send the GET request asynchronously and retrieve the response as a string.
            try
            {
                // Send the GET request
                HttpResponseMessage httpResponse = await client.GetAsync(new Uri(url));
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message);
            }

            return httpResponseBody;
        }

        /// <summary>
        /// Creates a file from an image URL's contents.
        /// </summary>
        /// <param name="url">URL to get the file from.</param>
        /// <param name="filename">Name of the file to save.</param>
        /// <returns>The filename without extension if success, "/" otherwise.</returns>
        public static async Task<string> SaveImageFromURLAsync(string url, string filename)
        {
            HttpClient client = new HttpClient();

            StorageFile tempFile = await ApplicationData.Current.LocalCacheFolder.
                CreateFileAsync("tempboi", CreationCollisionOption.GenerateUniqueName);

            StorageFile destinationFile = await ApplicationData.Current.LocalFolder.
                CreateFileAsync(filename.AsValidFileName(), CreationCollisionOption.GenerateUniqueName);

            bool result = false;
            string path = "/";
            try
            {
                IBuffer buffer = await client.GetBufferAsync(new Uri(url));
                using (IRandomAccessStream strm = await
                    tempFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    _ = await strm.WriteAsync(buffer);

                    // Create the decoder from the stream
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(strm);

                    // Get the SoftwareBitmap representation of the file
                    SoftwareBitmap softBmp = await decoder.GetSoftwareBitmapAsync();
                    result = await FileHelpers.SaveSoftwareBitmapToFile(softBmp, destinationFile);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when getting the image: " + ex.Message);
            }

            await tempFile.DeleteAsync();
            if (result)
            {
                path = Path.GetFileNameWithoutExtension(destinationFile.Path);
                return path;
            }

            await destinationFile.DeleteAsync();
            return path;
        }

        /// <summary>
        /// Checks whether or not the URL points to an image.
        /// </summary>
        /// <param name="url">URL to check</param>
        /// <returns>Whether or not the URL points to an image.</returns>
        public static async Task<bool> IsImageURLAsync(string url)
        {
            HttpClient client = new HttpClient();

            // Send the GET request asynchronously and retrieve the response as a string.
            try
            {
                // Send the GET request
                HttpResponseMessage httpResponse = await client.GetAsync(new Uri(url));
                httpResponse.EnsureSuccessStatusCode();
                HttpMediaTypeHeaderValue type = httpResponse.Content.Headers.ContentType;

                if (type.MediaType.ToLower(CultureInfo.InvariantCulture)
                    .StartsWith("image/"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }

    public static class VisualTreeHelpers
    {
        /// <summary>
        /// Tries to find the specified visual child.
        /// </summary>
        /// <typeparam name="childItem">The kind of item to find.</typeparam>
        /// <param name="obj">Object where search will happen.</param>
        /// <returns>The item if it's found, null otherwise.</returns>
        public static childItem FindVisualChild<childItem>(this DependencyObject obj)
            where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem item)
                {
                    return item;
                }
                else
                {
                    childItem childOfChild = child.FindVisualChild<childItem>();

                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to find all children of specified kind.
        /// </summary>
        /// <param name="parent">Where to find the item.</param>
        /// <param name="recurse">Whether or not the search should be recursive.</param>
        /// <returns>All child items found.</returns>
        public static IEnumerable<childItem> GetChildren<childItem>
            (this DependencyObject parent, bool recurse = false)
                where childItem : DependencyObject
        {
            if (parent != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    // Retrieve child visual at specified index value.
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child != null && child is childItem item)
                    {
                        yield return item;

                        if (recurse)
                        {
                            foreach (var grandChild in child.GetChildren<childItem>(true))
                            {
                                yield return grandChild;
                            }
                        }
                    }
                }
            }
        }
    }
}