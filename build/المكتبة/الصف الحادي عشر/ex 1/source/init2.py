import sqlite3


def transfer_images():
    # 1. الاتصال بقاعدة البيانات الهدف (التي تريد النقل إليها)
    # نستخدم r والمسار كما اتفقنا لتجنب رسائل التنبيه
    target_db = r'..\grade11_books.db'
    source_db = r'_grade11_books.db'

    try:
        conn = sqlite3.connect(target_db)
        cursor = conn.cursor()

        # 2. ربط قاعدة البيانات المصدر (التي تحتوي على الصور) بالاتصال الحالي
        # سنعطيها اسماً مستعاراً وليكن 'old_db'
        cursor.execute(f"ATTACH DATABASE '{source_db}' AS old_db")

        print("جاري نقل الصور بناءً على تطابق الـ id...")

        # 3. تنفيذ عملية التحديث لنقل الصور
        # سننقل من جدول book (في القاعدة القديمة) إلى جدول books (في القاعدة الجديدة)
        sql_update = '''
        UPDATE books
        SET cover = (
            SELECT cover 
            FROM old_db.book 
            WHERE old_db.book.id = books.id
        )
        WHERE EXISTS (
            SELECT 1 
            FROM old_db.book 
            WHERE old_db.book.id = books.id
        );
        '''

        cursor.execute(sql_update)

        # حفظ التغييرات
        conn.commit()

        # معرفة عدد الصفوف التي تم تحديثها
        print(f"تم بنجاح نقل الصور لعدد {cursor.rowcount} كتاب.")

        # 4. فصل قاعدة البيانات المصدر وإغلاق الاتصال
        cursor.execute("DETACH DATABASE old_db")
        conn.close()
        print("اكتملت العملية بنجاح.")

    except sqlite3.Error as e:
        print(f"حدث خطأ أثناء النقل: {e}")


if __name__ == "__main__":
    transfer_images()