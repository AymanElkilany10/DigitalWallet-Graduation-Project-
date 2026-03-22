import 'package:flutter/material.dart';

class AppColors {
  static const primary = Color(0xFF4F6EF7);
  static const success = Color(0xFF22C55E);
  static const danger = Color(0xFFEF4444);
  static const pending = Color(0xFFF59E0B);
  static const background = Color(0xFFF7F8FC);
  static const cardBg = Color(0xFFFFFFFF);
  static const textDark = Color(0xFF1A1D2E);
  static const textMuted = Color(0xFF9CA3AF);
  static const border = Color(0xFFE5E7EB);
  static const inputBg = Color(0xFFF9FAFB);
  static const darkCard = Color(0xFF1A1D2E);
}

class AppTheme {
  static ThemeData get theme => ThemeData(
        primaryColor: AppColors.primary,
        scaffoldBackgroundColor: AppColors.background,
        fontFamily: 'Nunito',
        appBarTheme: const AppBarTheme(
          backgroundColor: AppColors.cardBg,
          elevation: 0,
          centerTitle: true,
          iconTheme: IconThemeData(color: AppColors.textDark),
          titleTextStyle: TextStyle(
            color: AppColors.textDark,
            fontSize: 16,
            fontWeight: FontWeight.w700,
            fontFamily: 'Nunito',
          ),
        ),
      );
}
