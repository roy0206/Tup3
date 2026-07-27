using System.Collections.Generic;
using System.Text;

public static class CsvParser
{
    public static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // 다음 문자도 따옴표면 → 이스케이프된 따옴표("")
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++; // 하나 건너뜀
                    }
                    else
                    {
                        inQuotes = false; // 따옴표 닫힘
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRow.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\r')
                {
                    // \r 은 무시 (\r\n 대응)
                }
                else if (c == '\n')
                {
                    currentRow.Add(field.ToString());
                    field.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                }
                else
                {
                    field.Append(c);
                }
            }
        }

        // 마지막 줄 처리 (파일 끝에 개행이 없을 수 있음)
        if (field.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(field.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }
}