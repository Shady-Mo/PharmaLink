import re
import os

def process_sql_insert(line):
    # نتحقق من أن السطر يحتوي على جملة إدخال
    if not line.strip().startswith("INSERT INTO"):
        return line
        
    # نستخرج الجزء الخاص بالـ VALUES
    match = re.search(r"VALUES\s*\((.*)\);", line, re.DOTALL)
    if not match:
        return line
        
    values_str = match.group(1)
    
    # نقوم بفصل القيم مع مراعاة النصوص الموجودة داخل علامات الاقتباس الفردية '
    pattern = re.compile(r",\s*(?=(?:[^']*'[^']*')*[^']*$)")
    values = pattern.split(values_str)
    
    # نتحقق أن عدد الأعمدة كافٍ (جدول الأدوية يحتوي على 49 عمود)
    if len(values) >= 13:
        brand_name = values[2].strip("N' ")
        
        if "|" in brand_name:
            parts = [p.strip() for p in brand_name.split("|")]
            
            # تحديث GenericName إذا كان فارغاً
            if values[1] == "N''":
                values[1] = f"N'{parts[0].replace("'", "''")}'"
                
            if len(parts) >= 3:
                # تحديث Strength
                if values[6] == "N''":
                    values[6] = f"N'{parts[1].replace("'", "''")}'"
                # تحديث Form
                if values[7] == "N''":
                    values[7] = f"N'{parts[2].replace("'", "''")}'"
            elif len(parts) == 2:
                # تحديث Form
                if values[7] == "N''":
                    values[7] = f"N'{parts[1].replace("'", "''")}'"
                    
            # تحديث Manufacturer إذا كان فارغاً
            if values[12] == "N''":
                values[12] = f"N'{parts[0].replace("'", "''")}'"
                
        # إعادة تجميع السطر من جديد
        new_values_str = ", ".join(values)
        return line[:match.start(1)] + new_values_str + ");" + line[match.end():]
        
    return line

def main():
    # الحصول على مسار المجلد الحالي
    script_dir = os.path.dirname(os.path.abspath(__file__))
    input_file = os.path.join(script_dir, 'data.sql')
    output_file = os.path.join(script_dir, 'cleaned_data.sql')
    
    if not os.path.exists(input_file):
        print(f"❌ لم يتم العثور على ملف باسم data.sql")
        print(f"رجاءً قم بإنشاء ملف data.sql وضع فيه الأكواد ثم حاول مرة أخرى.")
        input("اضغط زر Enter للخروج...")
        return

    try:
        with open(input_file, 'r', encoding='utf-8') as infile, \
             open(output_file, 'w', encoding='utf-8') as outfile:
            for line in infile:
                processed_line = process_sql_insert(line)
                outfile.write(processed_line)
                
        print(f"✅ تم الانتهاء بنجاح!")
        print(f"تم حفظ البيانات المعدلة في ملف: cleaned_data.sql")
    except Exception as e:
        print(f"❌ حدث خطأ غير متوقع: {e}")
        
    input("اضغط زر Enter للخروج...")

if __name__ == "__main__":
    main()
