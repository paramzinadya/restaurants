﻿using System;
using System.Drawing;
using System.Windows.Forms;

public class HelpLibrary
{
    // Метод для вызова окна справки
    public static void GetHelp(int userId)
    {
        string message = "Контактная информация:\n" +
                         "телефон: +79537694750, email: parramzina@mail.ru.\n\n" +
                         "Пользователю отображаются доступные пункты меню сверху. При выборе раздела меню " +
                         "пользователю будут доступны чтение, редактирование, запись и удаление, в зависимости " +
                         "от предоставленного администратором доступа.\n\n" +
                         "В каждом разделе представлены данные таблиц, а также кнопки с названием, описывающее " +
                         "их действие. Кнопки не будут отображаться, если у Вас нет доступа к этому функционалу, " +
                         "при необходимости получения доступа необходимо обратиться к администратору.\n\n" +
                         "Кнопки \"Редактировать\" и \"Удалить\" активируются после выбора всей строки.\n\n" +
                         "Любые изменения: добавить/удалить/редактировать происходят единожды.";

        string caption = "Справка";

        MessageBox.Show(
            message,
            caption,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    public static void GetAbout(int userId)
    {
        string message = "Описание: Данное приложение позволяет оптимизировать процессы поставки и хранения продуктов, а также сокращение времени и ресурсов, затрачиваемые на учет и контроль\n\n" +
                         "Версия приложения: 1.0.0\n" +
                         "Разработчик: Парамзина Надежда Евгеньевна\n" +
                         "Контактная информация: телефон +7-953-769-47-50\n" +
                         "Для дополнительной информации вы можете обратиться к разделу 'Справка'";
        string caption = "О программе";

        MessageBox.Show(
            message,
            caption,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}


